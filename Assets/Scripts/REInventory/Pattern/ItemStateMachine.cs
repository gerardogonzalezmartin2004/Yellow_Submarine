using UnityEngine;

/// <summary>
/// State Pattern: Define los diferentes estados en los que puede estar un InventoryItem.
/// </summary>
public enum ItemState
{
    /// <summary>
    /// Item está colocado en un grid (posición estable).
    /// </summary>
    Placed,

    /// <summary>
    /// Item está siendo arrastrado por el jugador.
    /// </summary>
    BeingDragged,

    /// <summary>
    /// Item está volviendo automáticamente a su última posición válida.
    /// </summary>
    ReturningToLastPosition,

    /// <summary>
    /// Item está flotando (nunca ha sido colocado).
    /// </summary>
    Floating
}

/// <summary>
/// Máquina de estados para InventoryItem.
/// Gestiona transiciones entre estados y comportamientos específicos.
/// </summary>
public class ItemStateMachine
{
    #region Private Fields

    private ItemState currentState;
    private InventoryItem owner;

    #endregion

    #region Properties

    public ItemState CurrentState => currentState;

    public bool IsPlaced => currentState == ItemState.Placed;
    public bool IsBeingDragged => currentState == ItemState.BeingDragged;
    public bool IsReturning => currentState == ItemState.ReturningToLastPosition;
    public bool IsFloating => currentState == ItemState.Floating;

    #endregion

    #region Constructor

    public ItemStateMachine(InventoryItem owner, ItemState initialState = ItemState.Floating)
    {
        this.owner = owner;
        this.currentState = initialState;
    }

    #endregion

    #region State Transitions

    /// <summary>
    /// Cambia al estado especificado.
    /// Ejecuta OnExit del estado actual y OnEnter del nuevo estado.
    /// </summary>
    public void TransitionTo(ItemState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"[ItemStateMachine] Intentando transicionar al mismo estado: {newState}");
            return;
        }

        // Validar transición
        if (!IsValidTransition(currentState, newState))
        {
            Debug.LogError($"[ItemStateMachine] Transición inválida: {currentState} → {newState}");
            return;
        }

        ItemState previousState = currentState;

        // OnExit del estado anterior
        OnExitState(previousState);

        // Cambiar estado
        currentState = newState;

        // OnEnter del nuevo estado
        OnEnterState(newState);

        Debug.Log($"[ItemStateMachine] Transición: {previousState} → {newState}");
    }

    /// <summary>
    /// Valida si una transición es permitida.
    /// </summary>
    private bool IsValidTransition(ItemState from, ItemState to)
    {
        // Definir transiciones válidas
        switch (from)
        {
            case ItemState.Floating:
                return to == ItemState.BeingDragged || to == ItemState.Placed;

            case ItemState.Placed:
                return to == ItemState.BeingDragged;

            case ItemState.BeingDragged:
                return to == ItemState.Placed ||
                       to == ItemState.ReturningToLastPosition ||
                       to == ItemState.Floating;

            case ItemState.ReturningToLastPosition:
                return to == ItemState.Placed || to == ItemState.BeingDragged;

            default:
                return false;
        }
    }

    #endregion

    #region State Behaviors

    /// <summary>
    /// Se ejecuta al entrar en un nuevo estado.
    /// </summary>
    private void OnEnterState(ItemState state)
    {
        switch (state)
        {
            case ItemState.Placed:
                OnEnterPlaced();
                break;

            case ItemState.BeingDragged:
                OnEnterBeingDragged();
                break;

            case ItemState.ReturningToLastPosition:
                OnEnterReturning();
                break;

            case ItemState.Floating:
                OnEnterFloating();
                break;
        }
    }

    /// <summary>
    /// Se ejecuta al salir de un estado.
    /// </summary>
    private void OnExitState(ItemState state)
    {
        switch (state)
        {
            case ItemState.Placed:
                OnExitPlaced();
                break;

            case ItemState.BeingDragged:
                OnExitBeingDragged();
                break;

            case ItemState.ReturningToLastPosition:
                OnExitReturning();
                break;

            case ItemState.Floating:
                OnExitFloating();
                break;
        }
    }

    // --- Enter Behaviors ---

    private void OnEnterPlaced()
    {
        // Item colocado: podría cambiar color, desactivar efectos, etc.
        // TODO: Opcional - efecto visual de "colocado"
    }

    private void OnEnterBeingDragged()
    {
        // Item arrastrado: SetAsLastSibling para que se dibuje encima
        if (owner != null)
        {
            RectTransform rt = owner.GetComponent<RectTransform>();
            rt?.SetAsLastSibling();
        }

        // TODO: Opcional - efecto visual de "arrastrando" (glow, scale up, etc.)
    }

    private void OnEnterReturning()
    {
        // Item volviendo: podría iniciar animación, sonido, etc.
        // TODO: Opcional - sonido de "snap back"
    }

    private void OnEnterFloating()
    {
        // Item flotando: estado inicial cuando se crea
    }

    // --- Exit Behaviors ---

    private void OnExitPlaced()
    {
        // Saliendo de estado colocado
    }

    private void OnExitBeingDragged()
    {
        // Dejamos de arrastrar
    }

    private void OnExitReturning()
    {
        // Terminó de volver
    }

    private void OnExitFloating()
    {
        // Ya no está flotando
    }

    #endregion

    #region Debug

    public override string ToString()
    {
        return $"[ItemStateMachine: {currentState}]";
    }

    #endregion
}