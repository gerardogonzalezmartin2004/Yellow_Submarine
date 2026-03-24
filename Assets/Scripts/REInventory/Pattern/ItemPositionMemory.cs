using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Sistema de memoria de posición para InventoryItem.
/// Guarda el historial de posiciones y permite retorno automático a la última posición válida.
/// Integra los patrones: Memento, State, Strategy y Object Pool.
/// </summary>
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

    /// <summary>
    /// Referencia al InventoryItem dueño.
    /// </summary>
    private InventoryItem item;

    /// <summary>
    /// Máquina de estados del item.
    /// </summary>
    private ItemStateMachine stateMachine;

    /// <summary>
    /// Última posición válida (memento más reciente).
    /// </summary>
    private ItemMemento lastValidPosition;

    /// <summary>
    /// Historial completo de posiciones (para undo múltiple).
    /// </summary>
    private Stack<ItemMemento> positionHistory;

    /// <summary>
    /// Estrategia actual de retorno.
    /// </summary>
    private IReturnStrategy currentStrategy;

    /// <summary>
    /// Grid donde está actualmente el item (puede ser null si está en la mano).
    /// </summary>
    private ItemGrid currentGrid;

    /// <summary>
    /// Corrutina de animación activa (si hay una).
    /// </summary>
    private Coroutine activeReturnCoroutine;

    #endregion

    #region Properties

    /// <summary>
    /// Indica si el item tiene una posición válida a la que puede volver.
    /// </summary>
    public bool HasValidReturnPosition => lastValidPosition != null && lastValidPosition.IsValid;

    /// <summary>
    /// Estado actual del item.
    /// </summary>
    public ItemState CurrentState => stateMachine.CurrentState;

    /// <summary>
    /// Número de posiciones guardadas en el historial.
    /// </summary>
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

        LogDebug($"ItemPositionMemory inicializado con estrategia: {currentStrategy.StrategyName}");
    }

    #endregion

    #region Public API - Memento Management

    /// <summary>
    /// Guarda la posición actual del item como un memento.
    /// Se llama cuando el item se coloca exitosamente en un grid.
    /// </summary>
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

    /// <summary>
    /// Marca el item como "recogido" (empieza a ser arrastrado).
    /// </summary>
    public void MarkAsPickedUp()
    {
        stateMachine.TransitionTo(ItemState.BeingDragged);
        currentGrid = null;

        LogDebug("Item recogido - estado: BeingDragged");
    }

    /// <summary>
    /// Intenta volver el item a su última posición válida.
    /// Retorna true si el retorno fue exitoso.
    /// </summary>
    public bool ReturnToLastPosition()
    {
        if (!HasValidReturnPosition)
        {
            Debug.LogWarning("[ItemPositionMemory] No hay posición válida a la que volver");
            return false;
        }

        LogDebug($"Intentando retornar a última posición: {lastValidPosition}");

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

            LogDebug("Retorno exitoso");
        }
        else
        {
            Debug.LogError("[ItemPositionMemory] Fallo al retornar item");

            // Volver a estado BeingDragged si falló
            stateMachine.TransitionTo(ItemState.BeingDragged);
        }

        return success;
    }

    /// <summary>
    /// Intenta volver al penúltimo memento (undo).
    /// Solo funciona si enableFullHistory está activo.
    /// </summary>
    public bool UndoToPreviousPosition()
    {
        if (!enableFullHistory || positionHistory == null || positionHistory.Count < 2)
        {
            Debug.LogWarning("[ItemPositionMemory] No hay historial suficiente para undo");
            return false;
        }

        // Remover el memento actual (último)
        positionHistory.Pop();

        // El nuevo "último" es el anterior
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

    /// <summary>
    /// Cambia la estrategia de retorno en runtime.
    /// </summary>
    public void SetReturnStrategy(ReturnStrategyFactory.StrategyType newStrategy)
    {
        returnStrategy = newStrategy;
        currentStrategy = ReturnStrategyFactory.CreateStrategy(newStrategy);

        LogDebug($"Estrategia cambiada a: {currentStrategy.StrategyName}");
    }

    /// <summary>
    /// Limpia todo el historial de posiciones.
    /// </summary>
    public void ClearHistory()
    {
        lastValidPosition = null;
        currentGrid = null;

        if (positionHistory != null)
        {
            positionHistory.Clear();
        }

        stateMachine.TransitionTo(ItemState.Floating);

        LogDebug("Historial limpiado");
    }

    #endregion

    #region Animation Coroutines (para LerpReturnStrategy)

    /// <summary>
    /// Corrutina de animación Lerp (movimiento suave).
    /// Se usa con LerpReturnStrategy.
    /// </summary>
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

    /// <summary>
    /// Corrutina de animación Bounce (con rebote).
    /// Se usa con BounceReturnStrategy.
    /// </summary>
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

    /// <summary>
    /// Retorna información de debug del estado actual.
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"=== ItemPositionMemory Debug ===\n";
        info += $"Estado: {stateMachine.CurrentState}\n";
        info += $"Tiene posición válida: {HasValidReturnPosition}\n";
        info += $"Estrategia: {currentStrategy.StrategyName}\n";
        info += $"Historial habilitado: {enableFullHistory}\n";
        info += $"Items en historial: {HistoryCount}\n";

        if (HasValidReturnPosition)
        {
            info += $"Última posición: {lastValidPosition}\n";
        }

        return info;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxHistorySize < 1)
        {
            maxHistorySize = 1;
        }
    }
#endif

    #endregion
}