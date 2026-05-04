using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using AbyssalReach.Core;

public class InventoryController : MonoBehaviour
{
    // este script controla la lógica principal del inventario: abrir/cerrar, drag & drop, transferencia de loot del buzo al barco, y el sistema de highlight para mostrar dónde se colocaría el item.
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

    [Header("Drop Zone")]
    [Tooltip("Grid de descarte. Los items aquí se destruyen al cerrar el inventario.")]
    [SerializeField] private DropZoneGrid dropZoneGrid;

    #endregion

    #region Private Fields

    private ItemGrid selectedItemGrid;
    private InventoryItem selectedItem;
    private InventoryItem overlapItem;
    private RectTransform heldItemRect;
    private InventotyHighlight inventoryHighlight;

    private Vector2Int lastHighlightPos = new Vector2Int(-1, -1);
    private int lastRotationIdx = -1;
    private InventoryItem itemUnderCursor;

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
                if (selectedItem == null)
                    PickUpItem(tile);
                else
                    PlaceItem(tile);
            }
            else if (selectedItem != null)
            {
                // Click fuera de cualquier grid con item en mano → devolver a última posición.
                ReturnSelectedItemToLastPosition();
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
            // No desactivar — pertenece al Action Map Global.
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
        else
            Debug.LogWarning("[InventoryController] GameController.Instance es null");
    }

    #endregion

    #region Public API

    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvas == null) return;

        if (!visible)
        {
            // 1. Item en mano al cerrar → devolver a última posición válida.
            if (selectedItem != null)
            {
                ItemPositionMemory memory = selectedItem.GetComponent<ItemPositionMemory>();

                if (memory != null && memory.HasValidReturnPosition)
                {
                    memory.ReturnToLastPosition();
                    Debug.Log("[InventoryController] Item devuelto al cerrar inventario.");
                }
                else
                {
                    Destroy(selectedItem.gameObject);
                    Debug.LogWarning("[InventoryController] Item sin posición previa destruido al cerrar.");
                }

                selectedItem = null;
                heldItemRect = null;
            }

            // 2. Descartar todo lo que esté en el drop zone al cerrar.
            if (dropZoneGrid != null && !dropZoneGrid.IsEmpty)
            {
                Debug.Log("[InventoryController] Descartando items del drop zone...");
                dropZoneGrid.DiscardAllItems();
            }
        }

        inventoryCanvas.SetActive(visible);

        if (visible)
        {
            if (InventoryManager.Instance != null)
                TransferDiverLoot(InventoryManager.Instance.GetDiverInventory());
        }
    }

    public void TransferDiverLoot(DiverInventory diverInv)
    {
        if (diverInv == null) { Debug.LogWarning("[InventoryController] DiverInventory null"); return; }
        if (boatItemGrid == null) { Debug.LogError("[InventoryController] boatItemGrid no asignado"); return; }
        if (itemPrefab == null) { Debug.LogError("[InventoryController] itemPrefab no asignado"); return; }
        if (canvasTransform == null) { Debug.LogError("[InventoryController] canvasTransform no asignado"); return; }

        List<ItemData> diverItems = new List<ItemData>(diverInv.GetItems());
        List<ItemData> transferidos = new List<ItemData>();

        foreach (ItemData loot in diverItems)
        {
            if (loot == null) continue;

            InventoryItem newItem = Instantiate(itemPrefab, canvasTransform)
                                    .GetComponent<InventoryItem>();
            if (newItem == null) { Debug.LogError("[InventoryController] prefab sin InventoryItem"); continue; }

            newItem.Set(loot);

            Vector2Int? slot = boatItemGrid.FindSpaceForObject(newItem);

            if (slot == null)
            {
                Debug.LogWarning("[InventoryController] Sin espacio para: " + loot.name);
                Destroy(newItem.gameObject);
                continue;
            }

            boatItemGrid.PlaceItem(newItem, slot.Value.x, slot.Value.y);

            ItemPositionMemory memory = newItem.GetComponent<ItemPositionMemory>();
            memory?.SaveCurrentPosition(boatItemGrid);

            transferidos.Add(loot);
        }

        foreach (ItemData loot in transferidos)
            diverInv.GetItems().Remove(loot);

        Debug.Log("[InventoryController] Transferencia: " + transferidos.Count + "/" + diverItems.Count);
    }

    #endregion

    #region Drag & Drop

    private void PickUpItem(Vector2Int tile)
    {
        selectedItem = selectedItemGrid.PickUpItem(tile.x, tile.y);
        if (selectedItem == null) return;

        heldItemRect = selectedItem.GetComponent<RectTransform>();
        heldItemRect?.SetAsLastSibling();

        ItemPositionMemory memory = selectedItem.GetComponent<ItemPositionMemory>();
        memory?.MarkAsPickedUp();

        dropZoneGrid?.RefreshVisual();
    }

    private void PlaceItem(Vector2Int tile)
    {
        if (selectedItem == null || selectedItemGrid == null) return;

        InventoryItem itemBeingPlaced = selectedItem;

        bool placed = selectedItemGrid.PlaceItem(selectedItem, tile.x, tile.y, ref overlapItem);

        if (!placed)
        {
            // ── FIX "2 sprites bloqueando":
            //    OverlapCheck devuelve false cuando la zona está ocupada por 2 items distintos
            //    (swap imposible). En ese caso auto-devolvemos el item a su última posición,
            //    en vez de dejarlo flotando indefinidamente en mano.
            ItemPositionMemory memory = itemBeingPlaced.GetComponent<ItemPositionMemory>();
            if (memory != null && memory.HasValidReturnPosition)
            {
                Debug.Log("[InventoryController] Colocación bloqueada por múltiples items — devolviendo a última posición.");
                ReturnSelectedItemToLastPosition();
            }
            // Si nunca fue colocado, se queda en mano para que el jugador elija.
            return;
        }

        // Colocación exitosa.
        ItemPositionMemory placedMemory = itemBeingPlaced.GetComponent<ItemPositionMemory>();
        placedMemory?.SaveCurrentPosition(selectedItemGrid);

        // Forzar posición visual exacta en la celda (corrige ghost sprite).
        SnapItemVisualToGrid(itemBeingPlaced, selectedItemGrid);

        dropZoneGrid?.RefreshVisual();

        selectedItem = null;
        heldItemRect = null;

        // Swap RE4: el item desplazado pasa a la mano.
        if (overlapItem != null)
        {
            selectedItem = overlapItem;
            overlapItem = null;
            heldItemRect = selectedItem.GetComponent<RectTransform>();
            heldItemRect?.SetAsLastSibling();

            ItemPositionMemory swapMemory = selectedItem.GetComponent<ItemPositionMemory>();
            swapMemory?.MarkAsPickedUp();
        }
    }

    // Devuelve el item en mano a su última posición válida.
    private void ReturnSelectedItemToLastPosition()
    {
        if (selectedItem == null) return;

        ItemPositionMemory memory = selectedItem.GetComponent<ItemPositionMemory>();

        if (memory != null && memory.HasValidReturnPosition)
        {
            bool returned = memory.ReturnToLastPosition();

            if (returned)
            {
                dropZoneGrid?.RefreshVisual();
                selectedItem = null;
                heldItemRect = null;
            }
            else
            {
                Debug.LogWarning("[InventoryController] ReturnToLastPosition falló. Item sigue en mano.");
            }
        }
        else
        {
            Debug.Log("[InventoryController] Item sin posición previa — debe colocarse en un grid.");
        }
    }

    // Reposiciona el RectTransform del item a la celda exacta del grid.
    // Necesario porque durante el drag el transform sigue al cursor,
    // y al hacer drop puede quedar desplazado visualmente respecto al array del grid.
    private void SnapItemVisualToGrid(InventoryItem item, ItemGrid grid)
    {
        if (item == null || grid == null) return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        if (itemRect == null) return;

        Vector2 correctPos = grid.CalculatePositionOnGrid(
            item,
            item.onGridPositionX,
            item.onGridPositionY
        );

        itemRect.SetParent(grid.GetComponent<RectTransform>());
        itemRect.localPosition = correctPos;
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
        if (dropZoneGrid == null) Debug.LogWarning("[InventoryController] dropZoneGrid no asignado (opcional)");
        if (toggleInventoryAction == null) Debug.LogWarning("[InventoryController] toggleInventoryAction no asignado");
        if (pointerPositionAction == null) Debug.LogWarning("[InventoryController] pointerPositionAction no asignado");
    }

    #endregion
}