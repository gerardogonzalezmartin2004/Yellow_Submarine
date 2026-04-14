using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Sistema de memoria de posición para InventoryItem.
// Guarda el historial de posiciones y permite retorno automático a la última posición válida.
// Integra los patrones: Memento, State, Strategy y Object Pool.
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
    [Tooltip("Mostrar logs de debugging")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Private Fields

    // Referencia al InventoryItem dueño.
    private InventoryItem item;

    // Máquina de estados del item.
    private ItemStateMachine stateMachine;

    // Última posición válida 
    private ItemMemento lastValidPosition;

    // Historial completo de posiciones
    private Stack<ItemMemento> positionHistory;

    // Estrategia actual de retorno.
    private IReturnStrategy currentStrategy;

    // Grid donde está actualmente el item 
    private ItemGrid currentGrid;

    /// Corrutina de animación activa 
    private Coroutine activeReturnCoroutine;

    #endregion

    #region Properties

    /// Indica si el item tiene una posición válida a la que puede volver.
    public bool HasValidReturnPosition => lastValidPosition != null && lastValidPosition.IsValid;

    // Estado actual del item.
    public ItemState CurrentState => stateMachine.CurrentState;

    // Número de posiciones guardadas en el historial.
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

        // Inicializar máquina de estados
        stateMachine = new ItemStateMachine(item, ItemState.Floating);

        // Inicializar historial si está habilitado
        if (enableFullHistory)
        {
            positionHistory = new Stack<ItemMemento>();
        }

        // Crear estrategia de retorno
        currentStrategy = ReturnStrategyFactory.CreateStrategy(returnStrategy);

    }

    #endregion

    #region Public API 

    // Guarda la posición actual del item como un memento.
    // Se llama cuando el item se coloca exitosamente en un grid.
    public void SaveCurrentPosition(ItemGrid grid)
    {
        if (item == null || grid == null)
        {
            Debug.LogWarning("[ItemPositionMemory] Item o grid es null en SaveCurrentPosition");
            return;
        }

        // Crear memento
        ItemMemento memento = ItemMemento.CreateMemento(item, grid);

        if (!memento.IsValid)
        {
            Debug.LogWarning("[ItemPositionMemory] Memento creado es inválido");
            return;
        }

        // Guardar como última posición válida
        lastValidPosition = memento;
        currentGrid = grid;

        // Añadir al historial si está habilitado
        if (enableFullHistory && positionHistory != null)
        {
            positionHistory.Push(memento);

            // Limitar tamaño del historial
            while (positionHistory.Count > maxHistorySize)
            {
                positionHistory.Pop();
            }
        }

        // Cambiar estado a Placed
        stateMachine.TransitionTo(ItemState.Placed);

        LogDebug($"Posición guardada: {memento}");
    }

    /// Marca el item como recogido
    public void MarkAsPickedUp()
    {
        stateMachine.TransitionTo(ItemState.BeingDragged);
        currentGrid = null;

        LogDebug("Item recogido - estado: BeingDragged");
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


        // Cambiar estado a Returning
        stateMachine.TransitionTo(ItemState.ReturningToLastPosition);

        // Ejecutar estrategia de retorno
        bool success = currentStrategy.ExecuteReturn(item, lastValidPosition);

        if (success)
        {
            // Restaurar grid actual
            currentGrid = lastValidPosition.SourceGrid;

            // Cambiar estado a Placed
            stateMachine.TransitionTo(ItemState.Placed);

        }
        else
        {
            Debug.LogError("[ItemPositionMemory] Fallo al retornar item");

            // Volver a estado BeingDragged si falló
            stateMachine.TransitionTo(ItemState.BeingDragged);
        }

        return success;
    }

    // Intenta volver al penúltimo memento.
    // Solo funciona si enableFullHistory está activo.
    public bool UndoToPreviousPosition()
    {
        if (!enableFullHistory || positionHistory == null || positionHistory.Count < 2)
        {
            Debug.LogWarning("[ItemPositionMemory] No hay historial suficiente para undo");
            return false;
        }

        // Remover el memento actual 
        positionHistory.Pop();

        // El nuevo último es el anterior
        ItemMemento previousMemento = positionHistory.Peek();

        if (!previousMemento.IsValid)
        {
            Debug.LogWarning("[ItemPositionMemory] Memento anterior es inválido");
            return false;
        }

        // Actualizar última posición válida
        lastValidPosition = previousMemento;

        // Ejecutar retorno
        return ReturnToLastPosition();
    }

    #endregion

    #region Public API - State Management

    // Cambia la estrategia de retorno en runtime.
    public void SetReturnStrategy(ReturnStrategyFactory.StrategyType newStrategy)
    {
        returnStrategy = newStrategy;
        currentStrategy = ReturnStrategyFactory.CreateStrategy(newStrategy);

    }

    // Limpia todo el historial de posiciones.

    public void ClearHistory()
    {
        lastValidPosition = null;
        currentGrid = null;

        if (positionHistory != null)
        {
            positionHistory.Clear();
        }

        stateMachine.TransitionTo(ItemState.Floating);

    }

    #endregion

    #region Animation Coroutines (para LerpReturnStrategy)

    // Corrutina de animación Lerp 
    // Se usa con LerpReturnStrategy.
    public IEnumerator AnimateLerpReturn(ItemMemento targetMemento, float duration)
    {
        if (item == null || targetMemento == null || !targetMemento.IsValid)
        {
            yield break;
        }

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 startPos = rect.localPosition;
        Vector2 targetPos = targetMemento.SourceGrid.CalculatePositionOnGrid(
            item,
            targetMemento.GridX,
            targetMemento.GridY
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease out cubic para suavidad
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            rect.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);

            yield return null;
        }

        // Asegurar posición final exacta
        rect.localPosition = targetPos;

        // Colocar en el grid
        targetMemento.RestoreItem(item);

        activeReturnCoroutine = null;
    }

    // Corrutina de animación Bounce 
    // Se usa con BounceReturnStrategy.
    public IEnumerator AnimateBounceReturn(ItemMemento targetMemento, float duration, float bounceAmount)
    {
        if (item == null || targetMemento == null || !targetMemento.IsValid)
        {
            yield break;
        }

        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect == null) yield break;

        Vector3 startPos = rect.localPosition;
        Vector2 targetPos = targetMemento.SourceGrid.CalculatePositionOnGrid(
            item,
            targetMemento.GridX,
            targetMemento.GridY
        );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Elastic ease out para bounce
            float smoothT = Mathf.Sin(t * Mathf.PI * bounceAmount) * Mathf.Pow(1f - t, 2f) + t;

            rect.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);

            yield return null;
        }

        rect.localPosition = targetPos;
        targetMemento.RestoreItem(item);

        activeReturnCoroutine = null;
    }

    #endregion

    #region Debug Helpers

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ItemPositionMemory] {message}");
        }
    }





    private void OnValidate()
    {
        if (maxHistorySize < 1)
        {
            maxHistorySize = 1;
        }
    }


    #endregion
}