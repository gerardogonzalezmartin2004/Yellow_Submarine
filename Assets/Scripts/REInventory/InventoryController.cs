using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AbyssalReach.Core;

public class InventoryController : MonoBehaviour
{
    // este script es el cerebro del sistema de inventario. Maneja la lógica de arrastrar y soltar, la memoria de posición, la transferencia de items entre el buzo y el barco, y la interacción con el highlight visual.
    #region Serialized Fields

    [Header("Input Actions")]
    [SerializeField] private InputActionReference rotateAction;
    [SerializeField] private InputActionReference pointerPositionAction;
    [SerializeField] private InputActionReference toggleInventoryAction;

    [Header("References")]
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private ItemGrid boatItemGrid;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform canvasTransform;

    [Header("Memory System")]
    [Tooltip("Si true, usa el sistema de memoria de posición")]
    [SerializeField] private bool enablePositionMemory = true;

    [Tooltip("Estrategia de retorno cuando falla colocación")]
    [SerializeField]
    private ReturnStrategyFactory.StrategyType returnStrategy =
        ReturnStrategyFactory.StrategyType.Lerp;

    [Header("Debug")]
    [Tooltip("Mostrar logs de debugging")]
    [SerializeField] private bool showDebugLogs = true;

    [Tooltip("Modo ultra-verbose para debugging profundo")]
    [SerializeField] private bool verboseDebug = false;

    #endregion

    #region Singleton

    private static InventoryController instance;
    public static InventoryController Instance => instance;

    private void OnAwake_Singleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    #endregion

    #region Private Fields

    private ItemGrid selectedItemGrid;
    private InventoryItem selectedItem;
    private InventoryItem overlapItem;
    private RectTransform heldItemRect;
    private InventotyHighlight inventoryHighlight;

    // Cache para optimización del highlight
    private Vector2Int lastHighlightPos = new Vector2Int(-1, -1);
    private int lastRotationIdx = -1;
    private InventoryItem itemUnderCursor;

    // Sistema de memoria de posición
    private ItemPositionMemory currentItemMemory;

    // Debugging
    private ItemGridDebugger gridDebugger;

    #endregion

    #region Properties

    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;
            inventoryHighlight?.SetParent(value);
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        OnAwake_Singleton();
        inventoryHighlight = GetComponent<InventotyHighlight>();
        if (inventoryHighlight == null)
            Debug.LogError("[InventoryController] InventotyHighlight no encontrado");

        boatItemGrid?.ForceInit();

        if (inventoryCanvas != null)
            inventoryCanvas.SetActive(false);

        if (boatItemGrid != null)
        {
            gridDebugger = boatItemGrid.GetComponent<ItemGridDebugger>();
        }

        ValidateReferences();
    }

    private void OnEnable() => RegisterInputs();
    private void OnDisable() => UnregisterInputs();

    private void Update()
    {
        if (selectedItem != null && heldItemRect != null)
            heldItemRect.position = GetPointerPosition();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (selectedItemGrid != null)
            {
                Vector2Int tile = GetTileGridPosition();
                if (selectedItem == null) PickUpItem(tile);
                else PlaceItem(tile);
            }
        }

        if (selectedItemGrid == null)
        {
            inventoryHighlight?.Show(false);
            return;
        }

        HandleHighlight();
    }

    #endregion

    #region Input Setup

    private void RegisterInputs()
    {
        if (rotateAction?.action != null)
        {
            rotateAction.action.Enable();
            rotateAction.action.performed += OnRotatePerformed;
        }

        if (pointerPositionAction?.action != null)
            pointerPositionAction.action.Enable();

        if (toggleInventoryAction?.action != null)
        {
            toggleInventoryAction.action.Enable();
            toggleInventoryAction.action.performed += OnToggleInventoryPerformed;
        }
    }

    private void UnregisterInputs()
    {
        if (rotateAction?.action != null)
        {
            rotateAction.action.performed -= OnRotatePerformed;
            rotateAction.action.Disable();
        }

        if (pointerPositionAction?.action != null)
            pointerPositionAction.action.Disable();

        if (toggleInventoryAction?.action != null)
        {
            toggleInventoryAction.action.performed -= OnToggleInventoryPerformed;
        }
    }

    #endregion

    #region Input Callbacks

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        if (selectedItem == null) return;
        selectedItem.Rotate();
        lastRotationIdx = -1;

    }

    // Transfiere los items del grid del barco de vuelta al inventario del buzo.

    private void TransferBoatItemsToDiver()
    {
        if (InventoryManager.Instance == null || boatItemGrid == null)
            return;

        DiverInventory diverInv = InventoryManager.Instance.GetDiverInventory();
        List<InventoryItem> itemsToRemove = new List<InventoryItem>();

        // Recopilar todos los items únicos del grid
        for (int x = 0; x < boatItemGrid.GetWidth(); x++)
        {
            for (int y = 0; y < boatItemGrid.GetHeight(); y++)
            {
                InventoryItem item = boatItemGrid.GetItem(x, y);

                // Evitar procesar el mismo item dos veces
                if (item != null && !itemsToRemove.Contains(item))
                {
                    itemsToRemove.Add(item);
                }
            }
        }
        // Transferir cada item al inventario del buzo
        foreach (InventoryItem item in itemsToRemove)
        {
            if (item.itemData != null)
            {
                diverInv.GetItems().Add(item.itemData);
            }
            // Destruir la instancia visual del grid
            Destroy(item.gameObject);
        }

        if (showDebugLogs && itemsToRemove.Count > 0)
            Debug.Log("[InventoryController] " + itemsToRemove.Count + " items transferidos de barco a buzo");

        InventoryManager.Instance.NotifyInventoryUpdate();
    }


    private void OnToggleInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (GameController.Instance != null)
            GameController.Instance.ToggleInventory();
    }

    #endregion

    #region Public API

    // Vende todos los items del grid del barco y los destruye.
    // Retorna el valor total vendido.

    public int SellAllBoatItems()
    {
        if (boatItemGrid == null)
            return 0;

        int totalValue = 0;
        List<InventoryItem> itemsToRemove = new List<InventoryItem>();

        // Recorrer todas las celdas del grid y recopilar items únicos
        for (int x = 0; x < boatItemGrid.GetWidth(); x++)
        {
            for (int y = 0; y < boatItemGrid.GetHeight(); y++)
            {
                InventoryItem item = boatItemGrid.GetItem(x, y);

                // Evitar contar el mismo item dos veces (ocupa múltiples celdas)
                if (item != null && !itemsToRemove.Contains(item))
                {
                    totalValue += item.itemData.value;
                    itemsToRemove.Add(item);
                }
            }
        }

        // Destruir todos los items
        foreach (InventoryItem item in itemsToRemove)
        {
            Destroy(item.gameObject);
        }

        if (showDebugLogs && totalValue > 0)
            Debug.Log("[InventoryController] Vendidos items del barco por " + totalValue + "G");

        return totalValue;
    }
    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvas == null) return;

        // Activamos o desactivamos el Canvas visualmente
        inventoryCanvas.SetActive(visible);

        if (visible)
        {
            // Traemos SOLO el nuevo loot que el buzo haya recogido.
            if (InventoryManager.Instance != null)
            {
                TransferDiverLoot(InventoryManager.Instance.GetDiverInventory());
            }
        }
        else
        {
            // AL CERRAR: 
            // Ya NO llamamos a TransferBoatItemsToDiver(), así protegemos las posiciones.

            // Prevención de errores: Si el jugador cierra el inventario mientras arrastraba algo.
            if (selectedItem != null)
            {
                // Intentamos devolverlo a su última posición válida
                bool returned = TryReturnItemToLastPosition();

                if (!returned)
                {
                    // Si por algún motivo falla, lo destruimos para evitar items fantasma
                    Destroy(selectedItem.gameObject);
                }

                // Limpiamos las referencias del ratón
                selectedItem = null;
                heldItemRect = null;
                currentItemMemory = null;

                // Ocultamos el highlight visual por si se quedó encendido
                if (inventoryHighlight != null) inventoryHighlight.Show(false);
            }
        }
    }

    public void TransferDiverLoot(DiverInventory diverInv)
    {
        if (diverInv == null || boatItemGrid == null || itemPrefab == null || canvasTransform == null)
            return;

        List<ItemData> diverItems = new List<ItemData>(diverInv.GetItems());
        List<ItemData> transferidos = new List<ItemData>();

        foreach (ItemData loot in diverItems)
        {
            if (loot == null) continue;

            InventoryItem newItem = Instantiate(itemPrefab, canvasTransform)
                                    .GetComponent<InventoryItem>();
            if (newItem == null) continue;

            newItem.Set(loot);

            if (enablePositionMemory)
            {
                ItemPositionMemory memory = newItem.gameObject.AddComponent<ItemPositionMemory>();
                memory.SetReturnStrategy(returnStrategy);
            }

            Vector2Int? slot = boatItemGrid.FindSpaceForObject(newItem);

            if (slot == null)
            {
                Destroy(newItem.gameObject);
                continue;
            }

            boatItemGrid.PlaceItem(newItem, slot.Value.x, slot.Value.y);

            if (enablePositionMemory)
            {
                ItemPositionMemory memory = newItem.GetComponent<ItemPositionMemory>();
                memory?.SaveCurrentPosition(boatItemGrid);
            }

            transferidos.Add(loot);
        }

        foreach (ItemData loot in transferidos)
            diverInv.GetItems().Remove(loot);


        if (gridDebugger != null && verboseDebug)
        {
            gridDebugger.ValidateGridIntegrity();
        }
    }

    #endregion

    #region Drag & Drop

    // Recoge un item del grid.
    private void PickUpItem(Vector2Int tile)
    {
        if (selectedItemGrid == null)
        {
            return;
        }


        InventoryItem itemAtTile = selectedItemGrid.GetItem(tile.x, tile.y);

        if (itemAtTile == null)
        {
            return;
        }

        if (verboseDebug && gridDebugger != null)
        {
            gridDebugger.InspectCell(tile.x, tile.y);
        }

        // Esto es crucial para items grandes que ocupan múltiples celdas
        int originX = itemAtTile.onGridPositionX;
        int originY = itemAtTile.onGridPositionY;


        // Recoger desde el origen
        selectedItem = selectedItemGrid.PickUpItem(originX, originY);

        if (selectedItem == null)
        {

            if (gridDebugger != null)
            {
                gridDebugger.DumpGridState();
                gridDebugger.ValidateGridIntegrity();
            }

            return;
        }


        heldItemRect = selectedItem.GetComponent<RectTransform>();
        heldItemRect?.SetAsLastSibling();

        //  Obtener componente de memoria
        if (enablePositionMemory)
        {
            currentItemMemory = selectedItem.GetComponent<ItemPositionMemory>();

            if (currentItemMemory != null)
            {
                currentItemMemory.MarkAsPickedUp();
            }

        }
    }

    // Intenta colocar el item en el grid.
    //  Retorno robusto cuando falla.
    private void PlaceItem(Vector2Int tile)
    {
        if (selectedItem == null || selectedItemGrid == null) return;


        //   Verificar que la posición está dentro del grid
        if (!selectedItemGrid.BoundaryCheck(tile.x, tile.y, selectedItem.WIDTH, selectedItem.HEIGHT))
        {

            TryReturnItemToLastPosition();
            return;
        }

        bool placed = selectedItemGrid.PlaceItem(selectedItem, tile.x, tile.y, ref overlapItem);

        if (placed)
        {


            //  Guardar nueva posición en memoria
            if (enablePositionMemory && currentItemMemory != null)
            {
                currentItemMemory.SaveCurrentPosition(selectedItemGrid);
            }

            selectedItem = null;
            heldItemRect = null;
            currentItemMemory = null;

            // Swap estilo RE4
            if (overlapItem != null)
            {

                selectedItem = overlapItem;
                overlapItem = null;
                heldItemRect = selectedItem.GetComponent<RectTransform>();
                heldItemRect?.SetAsLastSibling();

                // Actualizar memoria del item swapeado
                if (enablePositionMemory)
                {
                    currentItemMemory = selectedItem.GetComponent<ItemPositionMemory>();
                    currentItemMemory?.MarkAsPickedUp();
                }
            }

            // Validar integridad después de colocar
            if (gridDebugger != null && verboseDebug)
            {
                gridDebugger.ValidateGridIntegrity();
            }
        }
        else
        {
            bool returned = TryReturnItemToLastPosition();


        }
    }

    private bool TryReturnItemToLastPosition()
    {
        if (!enablePositionMemory)
        {
            return false;
        }

        if (currentItemMemory == null)
        {
            return false;
        }

        if (!currentItemMemory.HasValidReturnPosition)
        {
            return false;
        }


        bool returned = currentItemMemory.ReturnToLastPosition();

        if (returned)
        {

            // Limpiar selección
            selectedItem = null;
            heldItemRect = null;
            currentItemMemory = null;

            return true;
        }
        else
        {
            Debug.LogError(" Faloo al retornar item");

            // Debug profundo
            if (gridDebugger != null)
            {
                gridDebugger.DumpGridState();
            }

            return false;
        }
    }

    #endregion

    #region Highlight

    private void HandleHighlight()
    {
        if (inventoryHighlight == null || selectedItemGrid == null) return;

        Vector2Int currentPos = GetTileGridPosition();
        int currentRot = selectedItem != null ? selectedItem.RotationIndex : -1;

        if (currentPos == lastHighlightPos && currentRot == lastRotationIdx) return;

        lastHighlightPos = currentPos;
        lastRotationIdx = currentRot;

        if (selectedItem == null)
        {
            itemUnderCursor = selectedItemGrid.GetItem(currentPos.x, currentPos.y);
            if (itemUnderCursor != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemUnderCursor);
                inventoryHighlight.SetPosition(selectedItemGrid, itemUnderCursor);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        else
        {
            bool fits = selectedItemGrid.BoundaryCheck(
                currentPos.x, currentPos.y, selectedItem.WIDTH, selectedItem.HEIGHT);

            inventoryHighlight.Show(fits);
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, currentPos.x, currentPos.y);
        }
    }

    #endregion

    #region Helpers

    private Vector2Int GetTileGridPosition()
    {
        Vector2 pos = GetPointerPosition();

        if (selectedItem != null)
        {
            pos.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidht / 2f;
            pos.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2f;
        }

        return selectedItemGrid != null
            ? selectedItemGrid.GetTileGridPosition(pos)
            : Vector2Int.zero;
    }

    private Vector2 GetPointerPosition()
    {
        if (pointerPositionAction?.action != null)
            return pointerPositionAction.action.ReadValue<Vector2>();

        return Input.mousePosition;
    }

    private void ValidateReferences()
    {
        if (itemPrefab == null) Debug.LogWarning("[InventoryController] itemPrefab no asignado");
        if (canvasTransform == null) Debug.LogWarning("[InventoryController] canvasTransform no asignado");
        if (boatItemGrid == null) Debug.LogWarning("[InventoryController] boatItemGrid no asignado");
    }


    private void ContextValidateGrid()
    {
        if (gridDebugger != null)
        {
            gridDebugger.ValidateGridIntegrity();
        }
        else
        {
            Debug.LogWarning("No ItemGridDebugger found");
        }
    }


    #endregion

}