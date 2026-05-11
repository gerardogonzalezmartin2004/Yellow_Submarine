using UnityEngine;

public enum ItemState
{
    Placed,

    BeingDragged,

    ReturningToLastPosition,

    Floating
}


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

    
    public void TransitionTo(ItemState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"[ItemStateMachine] Intentando transicionar al mismo estado: {newState}");
            return;
        }

        if (!IsValidTransition(currentState, newState))
        {
            Debug.LogError($"[ItemStateMachine] Transición inválida: {currentState} → {newState}");
            return;
        }

        ItemState previousState = currentState;

        OnExitState(previousState);

        currentState = newState;

        OnEnterState(newState);

        Debug.Log($"[ItemStateMachine] Transición: {previousState} → {newState}");
    }

    private bool IsValidTransition(ItemState from, ItemState to)
    {
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
        
        if (owner != null)
        {
            RectTransform rt = owner.GetComponent<RectTransform>();
            rt?.SetAsLastSibling();
        }
    }



    private void OnEnterReturning()
    {

        //  Sonido de snap o algo del estilo
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
        // ya no está flotando
    }

    #endregion

    #region Debug



    #endregion
}