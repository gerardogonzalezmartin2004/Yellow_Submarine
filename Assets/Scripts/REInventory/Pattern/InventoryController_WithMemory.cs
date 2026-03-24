using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AbyssalReach.Core;

/// <summary>
/// InventoryController mejorado con sistema de memoria de posición.
/// Integra ItemPositionMemory para retorno automático cuando falla la colocación.
/// </summary>
public class InventoryController_WithMemory : MonoBehaviour
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
    [SerializeField] private bool showDebugLogs = false;

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
                // NUEVO: Si cerramos con item en mano, intentar retornar a última posición
                if (enablePositionMemory && currentItemMemory != null &&
                    currentItemMemory.HasValidReturnPosition)
                {
                    LogDebug("Inventario cerrado con item en mano - retornando a última posición");
                    currentItemMemory.ReturnToLastPosition();
                }
                else
                {
                    // Sin memoria, destruir el item
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
    }

    #endregion

    #region Drag & Drop (CON MEMORIA)

    private void PickUpItem(Vector2Int tile)
    {
        selectedItem = selectedItemGrid.PickUpItem(tile.x, tile.y);
        if (selectedItem == null) return;

        heldItemRect = selectedItem.GetComponent<RectTransform>();
        heldItemRect?.SetAsLastSibling();

        // NUEVO: Obtener componente de memoria
        if (enablePositionMemory)
        {
            currentItemMemory = selectedItem.GetComponent<ItemPositionMemory>();

            if (currentItemMemory != null)
            {
                currentItemMemory.MarkAsPickedUp();
                LogDebug($"Item recogido - memoria activa: {currentItemMemory.HasValidReturnPosition}");
            }
        }
    }

    private void PlaceItem(Vector2Int tile)
    {
        if (selectedItem == null || selectedItemGrid == null) return;

        bool placed = selectedItemGrid.PlaceItem(selectedItem, tile.x, tile.y, ref overlapItem);

        if (placed)
        {
            // COLOCACIÓN EXITOSA

            // NUEVO: Guardar nueva posición en memoria
            if (enablePositionMemory && currentItemMemory != null)
            {
                currentItemMemory.SaveCurrentPosition(selectedItemGrid);
                LogDebug($"Nueva posición guardada: ({tile.x}, {tile.y})");
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

                // NUEVO: Actualizar memoria del item swapeado
                if (enablePositionMemory)
                {
                    currentItemMemory = selectedItem.GetComponent<ItemPositionMemory>();
                    currentItemMemory?.MarkAsPickedUp();
                }
            }
        }
        else
        {
            // COLOCACIÓN FALLIDA - ACTIVAR RETORNO A ÚLTIMA POSICIÓN

            LogDebug("⚠️ Colocación fallida - intentando retorno automático");

            if (enablePositionMemory && currentItemMemory != null &&
                currentItemMemory.HasValidReturnPosition)
            {
                // Ejecutar retorno automático
                bool returned = currentItemMemory.ReturnToLastPosition();

                if (returned)
                {
                    LogDebug("✅ Item retornado automáticamente a última posición");

                    // Limpiar selección
                    selectedItem = null;
                    heldItemRect = null;
                    currentItemMemory = null;
                }
                else
                {
                    Debug.LogError("❌ Fallo al retornar item - destruyendo");
                    Destroy(selectedItem.gameObject);
                    selectedItem = null;
                    heldItemRect = null;
                    currentItemMemory = null;
                }
            }
            else
            {
                // Sin memoria o sin posición válida: el item sigue en la mano
                LogDebug("Item sigue en la mano (sin memoria de posición)");
            }
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

    #endregion
}