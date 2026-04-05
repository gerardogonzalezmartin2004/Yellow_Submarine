using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AbyssalReach.Core;

public class InventoryController : MonoBehaviour
{
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
        inventoryHighlight = GetComponent<InventotyHighlight>();
        if (inventoryHighlight == null)
            Debug.LogError("[InventoryController] InventotyHighlight no encontrado");

        boatItemGrid?.ForceInit();

        if (inventoryCanvas != null)
            inventoryCanvas.SetActive(false);

        // Obtener debugger si existe
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

        LogDebug($"Item rotado a {selectedItem.RotationIndex * 90}°");
    }

    private void OnToggleInventoryPerformed(InputAction.CallbackContext ctx)
    {
        if (GameController.Instance != null)
            GameController.Instance.ToggleInventory();
    }

    #endregion

    #region Public API

    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvas == null) return;

        inventoryCanvas.SetActive(visible);

        if (visible)
        {
            if (InventoryManager.Instance != null)
                TransferDiverLoot(InventoryManager.Instance.GetDiverInventory());
        }
        else
        {
            if (selectedItem != null)
            {
                // NUEVO: Mejorado - siempre intenta retornar antes de destruir
                bool returned = TryReturnItemToLastPosition();

                if (!returned)
                {
                    LogDebug("⚠️ No se pudo retornar item al cerrar - destruyendo");
                    Destroy(selectedItem.gameObject);
                }

                selectedItem = null;
                heldItemRect = null;
                currentItemMemory = null;
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

            // NUEVO: Añadir componente de memoria si está habilitado
            if (enablePositionMemory)
            {
                ItemPositionMemory memory = newItem.gameObject.AddComponent<ItemPositionMemory>();
                memory.SetReturnStrategy(returnStrategy);
                LogDebug($"ItemPositionMemory añadido a {loot.name}");
            }

            Vector2Int? slot = boatItemGrid.FindSpaceForObject(newItem);

            if (slot == null)
            {
                Debug.LogWarning($"[InventoryController] Sin espacio para: {loot.name}");
                Destroy(newItem.gameObject);
                continue;
            }

            boatItemGrid.PlaceItem(newItem, slot.Value.x, slot.Value.y);

            // NUEVO: Guardar posición inicial en memoria
            if (enablePositionMemory)
            {
                ItemPositionMemory memory = newItem.GetComponent<ItemPositionMemory>();
                memory?.SaveCurrentPosition(boatItemGrid);
            }

            transferidos.Add(loot);
            LogDebug($"Transferido al barco: {loot.name}");
        }

        foreach (ItemData loot in transferidos)
            diverInv.GetItems().Remove(loot);

        Debug.Log($"[InventoryController] Transferencia: {transferidos.Count}/{diverItems.Count}");

        // Validar integridad después de transferencia
        if (gridDebugger != null && verboseDebug)
        {
            gridDebugger.ValidateGridIntegrity();
        }
    }

    #endregion

    #region Drag & Drop (MEJORADO)

    /// <summary>
    /// Recoge un item del grid.
    /// MEJORADO: Ahora funciona en CUALQUIER celda del item, no solo en la celda de origen.
    /// </summary>
    private void PickUpItem(Vector2Int tile)
    {
        if (selectedItemGrid == null)
        {
            LogDebug("⚠️ PickUpItem: selectedItemGrid es null");
            return;
        }

        LogVerbose($"PickUpItem en tile ({tile.x}, {tile.y})");

        // CRÍTICO: Obtener el item en esa celda
        InventoryItem itemAtTile = selectedItemGrid.GetItem(tile.x, tile.y);

        if (itemAtTile == null)
        {
            LogVerbose("Celda vacía - no hay nada que recoger");
            return;
        }

        // NUEVO: Debug - inspeccionar celda si hay problemas
        if (verboseDebug && gridDebugger != null)
        {
            gridDebugger.InspectCell(tile.x, tile.y);
        }

        // MEJORADO: Recoger desde la posición de ORIGEN del item, no desde donde hicimos click
        // Esto es crucial para items grandes que ocupan múltiples celdas
        int originX = itemAtTile.onGridPositionX;
        int originY = itemAtTile.onGridPositionY;

        LogVerbose($"Item encontrado: {itemAtTile.itemData.name} @ origen ({originX}, {originY})");

        // Recoger desde el origen
        selectedItem = selectedItemGrid.PickUpItem(originX, originY);

        if (selectedItem == null)
        {
            Debug.LogError($"[InventoryController] ❌ FALLO al recoger item desde origen ({originX}, {originY})");

            // Debug profundo
            if (gridDebugger != null)
            {
                gridDebugger.DumpGridState();
                gridDebugger.ValidateGridIntegrity();
            }

            return;
        }

        LogDebug($"✅ Item recogido: {selectedItem.itemData.name}");

        heldItemRect = selectedItem.GetComponent<RectTransform>();
        heldItemRect?.SetAsLastSibling();

        // NUEVO: Obtener componente de memoria
        if (enablePositionMemory)
        {
            currentItemMemory = selectedItem.GetComponent<ItemPositionMemory>();

            if (currentItemMemory != null)
            {
                currentItemMemory.MarkAsPickedUp();
                LogDebug($"Memoria activa: {currentItemMemory.HasValidReturnPosition}");
            }
            else
            {
                LogDebug("⚠️ Item no tiene ItemPositionMemory");
            }
        }
    }

    /// <summary>
    /// Intenta colocar el item en el grid.
    /// MEJORADO: Retorno robusto cuando falla.
    /// </summary>
    private void PlaceItem(Vector2Int tile)
    {
        if (selectedItem == null || selectedItemGrid == null) return;

        LogVerbose($"PlaceItem en tile ({tile.x}, {tile.y})");

        // VALIDACIÓN PREVIA: Verificar que la posición está dentro del grid
        if (!selectedItemGrid.BoundaryCheck(tile.x, tile.y, selectedItem.WIDTH, selectedItem.HEIGHT))
        {
            LogDebug($"⚠️ Posición fuera de límites: ({tile.x}, {tile.y}) con tamaño {selectedItem.WIDTH}x{selectedItem.HEIGHT}");

            // NUEVO: Retorno inmediato cuando está fuera
            TryReturnItemToLastPosition();
            return;
        }

        bool placed = selectedItemGrid.PlaceItem(selectedItem, tile.x, tile.y, ref overlapItem);

        if (placed)
        {
            // ✅ COLOCACIÓN EXITOSA

            LogDebug($"✅ Item colocado exitosamente en ({tile.x}, {tile.y})");

            // NUEVO: Guardar nueva posición en memoria
            if (enablePositionMemory && currentItemMemory != null)
            {
                currentItemMemory.SaveCurrentPosition(selectedItemGrid);
                LogVerbose($"Nueva posición guardada en memoria");
            }

            selectedItem = null;
            heldItemRect = null;
            currentItemMemory = null;

            // Swap estilo RE4
            if (overlapItem != null)
            {
                LogDebug($"Swap detectado - recogiendo: {overlapItem.itemData.name}");

                selectedItem = overlapItem;
                overlapItem = null;
                heldItemRect = selectedItem.GetComponent<RectTransform>();
                heldItemRect?.SetAsLastSibling();

                // NUEVO: Actualizar memoria del item swapeado
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
            // ❌ COLOCACIÓN FALLIDA - ACTIVAR RETORNO

            LogDebug($"❌ Colocación fallida en ({tile.x}, {tile.y})");

            // NUEVO: Retorno robusto
            bool returned = TryReturnItemToLastPosition();

            if (!returned)
            {
                // Fallback: Item permanece en la mano
                LogDebug("Item permanece en la mano (sin posición válida de retorno)");
            }
        }
    }

    /// <summary>
    /// Intenta retornar el item a su última posición válida.
    /// Retorna true si el retorno fue exitoso.
    /// </summary>
    private bool TryReturnItemToLastPosition()
    {
        if (!enablePositionMemory)
        {
            LogDebug("Sistema de memoria desactivado");
            return false;
        }

        if (currentItemMemory == null)
        {
            LogDebug("Item no tiene ItemPositionMemory");
            return false;
        }

        if (!currentItemMemory.HasValidReturnPosition)
        {
            LogDebug("Item no tiene posición válida de retorno");
            return false;
        }

        LogDebug("🔄 Iniciando retorno a última posición...");

        bool returned = currentItemMemory.ReturnToLastPosition();

        if (returned)
        {
            LogDebug("✅ Item retornado exitosamente");

            // Limpiar selección
            selectedItem = null;
            heldItemRect = null;
            currentItemMemory = null;

            return true;
        }
        else
        {
            Debug.LogError("❌ FALLO al retornar item");

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

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[InventoryController] {message}");
        }
    }

    private void LogVerbose(string message)
    {
        if (verboseDebug)
        {
            Debug.Log($"[InventoryController|VERBOSE] {message}");
        }
    }

    #endregion

    #region Context Menu (Editor Only)

#if UNITY_EDITOR
    [ContextMenu("Dump Current State")]
    private void ContextDumpState()
    {
        Debug.Log("=== INVENTORY CONTROLLER STATE ===");
        Debug.Log($"Selected Item: {(selectedItem != null ? selectedItem.itemData.name : "None")}");
        Debug.Log($"Selected Grid: {(selectedItemGrid != null ? selectedItemGrid.name : "None")}");
        Debug.Log($"Memory Enabled: {enablePositionMemory}");
        Debug.Log($"Current Memory: {(currentItemMemory != null ? currentItemMemory.GetDebugInfo() : "None")}");

        if (gridDebugger != null)
        {
            gridDebugger.DumpGridState();
        }
    }

    [ContextMenu("Validate Grid Integrity")]
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
#endif

    #endregion
}