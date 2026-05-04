using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Sistema de memoria de posición para InventoryItem.
// Guarda el historial de posiciones y permite retorno automático a la última posición válida.
[RequireComponent(typeof(InventoryItem))]
public class ItemPositionMemory : MonoBehaviour
{
    #region Serialized Fields

    [Header("Configuration")]
    [Tooltip("Estrategia de retorno a usar")]
    [SerializeField] private ReturnStrategyFactory.StrategyType returnStrategy = ReturnStrategyFactory.StrategyType.Instant;

    [Tooltip("Si true, guarda un historial completo (permite undo múltiple)")]
    [SerializeField] private bool enableFullHistory = false;

    [Tooltip("Número máximo de mementos en el historial")]
    [SerializeField] private int maxHistorySize = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Private Fields

    private InventoryItem item;
    private ItemStateMachine stateMachine;
    private ItemMemento lastValidPosition;
    private Stack<ItemMemento> positionHistory;
    private IReturnStrategy currentStrategy;
    private ItemGrid currentGrid;
    private Coroutine activeReturnCoroutine;

    #endregion

    #region Properties

    public bool HasValidReturnPosition => lastValidPosition != null && lastValidPosition.IsValid;
    public ItemState CurrentState => stateMachine.CurrentState;
    public int HistoryCount => positionHistory?.Count ?? 0;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        item = GetComponent<InventoryItem>();

        if (item == null)
        {
            Debug.LogError("[ItemPositionMemory] No se encontró InventoryItem en " + gameObject.name);
            enabled = false;
            return;
        }

        stateMachine = new ItemStateMachine(item, ItemState.Floating);

        if (enableFullHistory)
            positionHistory = new Stack<ItemMemento>();

        currentStrategy = ReturnStrategyFactory.CreateStrategy(returnStrategy);
    }

    #endregion

    #region Public API

    // Guarda la posición actual como memento válido y transiciona a Placed.
    // Llamar después de que ItemGrid.PlaceItem tenga éxito.
    public void SaveCurrentPosition(ItemGrid grid)
    {
        if (item == null || grid == null)
        {
            Debug.LogWarning("[ItemPositionMemory] Item o grid es null en SaveCurrentPosition");
            return;
        }

        ItemMemento memento = ItemMemento.CreateMemento(item, grid);

        if (!memento.IsValid)
        {
            Debug.LogWarning("[ItemPositionMemory] Memento inválido al guardar posición");
            return;
        }

        lastValidPosition = memento;
        currentGrid = grid;

        if (enableFullHistory && positionHistory != null)
        {
            positionHistory.Push(memento);

            while (positionHistory.Count > maxHistorySize)
                positionHistory.Pop();
        }

        // ── FIX: Transición segura hacia Placed desde cualquier estado previo.
        //    SaveCurrentPosition puede llamarse desde Floating (transfer inicial),
        //    BeingDragged (drop normal) o ReturningToLastPosition.
        TransitionSafeTo(ItemState.Placed);

        LogDebug($"Posición guardada: grid=({memento.GridX},{memento.GridY}) rot={memento.RotationIndex}");
    }

    // Marca el item como recogido. Llamar al inicio de PickUpItem.
    public void MarkAsPickedUp()
    {
        // ── FIX: Solo transitamos si no estamos ya en BeingDragged.
        if (stateMachine.CurrentState != ItemState.BeingDragged)
            stateMachine.TransitionTo(ItemState.BeingDragged);

        currentGrid = null;
        LogDebug("Item marcado como recogido (BeingDragged)");
    }

    // Intenta volver el item a su última posición válida.
    // Retorna true si el retorno fue exitoso.
    public bool ReturnToLastPosition()
    {
        if (!HasValidReturnPosition)
        {
            Debug.LogWarning("[ItemPositionMemory] No hay posición válida a la que volver");
            return false;
        }

        // Aseguramos que estamos en BeingDragged antes de transicionar a Returning.
        if (stateMachine.CurrentState != ItemState.BeingDragged)
        {
            Debug.LogWarning($"[ItemPositionMemory] ReturnToLastPosition llamado en estado {stateMachine.CurrentState}. Forzando BeingDragged.");
            stateMachine.TransitionTo(ItemState.BeingDragged);
        }

        stateMachine.TransitionTo(ItemState.ReturningToLastPosition);

        bool success = currentStrategy.ExecuteReturn(item, lastValidPosition);

        if (success)
        {
            currentGrid = lastValidPosition.SourceGrid;
            stateMachine.TransitionTo(ItemState.Placed);
            LogDebug($"Retorno exitoso a ({lastValidPosition.GridX},{lastValidPosition.GridY})");
        }
        else
        {
            Debug.LogError("[ItemPositionMemory] Fallo al retornar item a última posición");
            stateMachine.TransitionTo(ItemState.BeingDragged);
        }

        return success;
    }

    // Undo a la posición anterior. Solo si enableFullHistory está activo.
    public bool UndoToPreviousPosition()
    {
        if (!enableFullHistory || positionHistory == null || positionHistory.Count < 2)
        {
            Debug.LogWarning("[ItemPositionMemory] No hay historial suficiente para undo");
            return false;
        }

        positionHistory.Pop();
        ItemMemento previousMemento = positionHistory.Peek();

        if (!previousMemento.IsValid)
        {
            Debug.LogWarning("[ItemPositionMemory] Memento anterior es inválido");
            return false;
        }

        lastValidPosition = previousMemento;
        return ReturnToLastPosition();
    }

    #endregion

    #region State Management

    public void SetReturnStrategy(ReturnStrategyFactory.StrategyType newStrategy)
    {
        returnStrategy = newStrategy;
        currentStrategy = ReturnStrategyFactory.CreateStrategy(newStrategy);
    }

    // ── FIX: ClearHistory transiciona por etapas para respetar la FSM.
    //    Placed → BeingDragged → Floating (las únicas rutas válidas).
    public void ClearHistory()
    {
        lastValidPosition = null;
        currentGrid = null;

        if (positionHistory != null)
            positionHistory.Clear();

        // Pasamos por BeingDragged si estamos en Placed, ya que Placed→Floating no es válido.
        if (stateMachine.CurrentState == ItemState.Placed ||
            stateMachine.CurrentState == ItemState.ReturningToLastPosition)
        {
            stateMachine.TransitionTo(ItemState.BeingDragged);
        }

        if (stateMachine.CurrentState != ItemState.Floating)
            stateMachine.TransitionTo(ItemState.Floating);

        LogDebug("Historial limpiado. Estado: Floating");
    }

    // Transición segura: si ya estamos en el estado destino, no hace nada.
    // Si la transición directa no es válida, intenta rutas intermedias conocidas.
    private void TransitionSafeTo(ItemState target)
    {
        if (stateMachine.CurrentState == target) return;

        ItemState current = stateMachine.CurrentState;

        // Ruta especial: Floating → Placed (no directo en la FSM, pasamos por BeingDragged)
        if (current == ItemState.Floating && target == ItemState.Placed)
        {
            stateMachine.TransitionTo(ItemState.BeingDragged);
            stateMachine.TransitionTo(ItemState.Placed);
            return;
        }

        // Intentar directamente (cubre BeingDragged→Placed, ReturningToLastPosition→Placed, etc.)
        stateMachine.TransitionTo(target);
    }

    #endregion

    #region Animation Coroutines

    public IEnumerator AnimateLerpReturn(ItemMemento targetMemento, float duration)
    {
        if (item == null || targetMemento == null || !targetMemento.IsValid)
            yield break;

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 startPos = rect.localPosition;
        Vector2 targetPos = targetMemento.SourceGrid.CalculatePositionOnGrid(
            item, targetMemento.GridX, targetMemento.GridY);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            rect.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        rect.localPosition = targetPos;
        targetMemento.RestoreItem(item);
        activeReturnCoroutine = null;
    }

    public IEnumerator AnimateBounceReturn(ItemMemento targetMemento, float duration, float bounceAmount)
    {
        if (item == null || targetMemento == null || !targetMemento.IsValid)
            yield break;

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 startPos = rect.localPosition;
        Vector2 targetPos = targetMemento.SourceGrid.CalculatePositionOnGrid(
            item, targetMemento.GridX, targetMemento.GridY);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = Mathf.Sin(t * Mathf.PI * bounceAmount) * Mathf.Pow(1f - t, 2f) + t;
            rect.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            yield return null;
        }

        rect.localPosition = targetPos;
        targetMemento.RestoreItem(item);
        activeReturnCoroutine = null;
    }

    #endregion

    #region Debug

    private void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[ItemPositionMemory:{gameObject.name}] {message}");
    }

    private void OnValidate()
    {
        if (maxHistorySize < 1)
            maxHistorySize = 1;
    }

    #endregion
}