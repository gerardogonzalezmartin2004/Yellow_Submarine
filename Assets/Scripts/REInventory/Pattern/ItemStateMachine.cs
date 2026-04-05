using UnityEngine;

// State Pattern: Define los diferentes estados en los que puede estar un InventoryItem.
public enum ItemState
{
    // Item está colocado en un grid (posición estable).
    Placed,

    // Item está siendo arrastrado por el jugador.
    BeingDragged,

    // Item está volviendo automáticamente a su última posición válida.
    ReturningToLastPosition,

    // Item está flotando (nunca ha sido colocado).
    Floating
}

// Máquina de estados para InventoryItem.
// Gestiona transiciones entre estados y comportamientos específicos.
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

    // Cambia al estado especificado.
    // Ejecuta OnExit del estado actual y OnEnter del nuevo estado.
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

    // Valida si una transición es permitida.
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

    // Se ejecuta al entrar en un nuevo estado.
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

    // Se ejecuta al salir de un estado.
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


    private void OnEnterPlaced()
    {
        // podría cambiar color, desactivar efectos, etc.
    }

    private void OnEnterBeingDragged()
    {
        // Item arrastrado: SetAsLastSibling para que se dibuje encima
        if (owner != null)
        {
            RectTransform rt = owner.GetComponent<RectTransform>();
            rt?.SetAsLastSibling();
        }

        // se podria aplicar efecto visual de arrastrando 
    }

    private void OnEnterReturning()
    {
       
        //  sonido de "snap back"
    }

    private void OnEnterFloating()
    {
        // cuando se crea
    }



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

   

    #endregion
}