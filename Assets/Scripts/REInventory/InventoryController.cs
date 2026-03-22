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
    // ToggleInventory vive en el Action Map Global que nunca se desactiva.
    // Su callback delega en GameController.ToggleInventory() para gestión de estado.
    [SerializeField] private InputActionReference toggleInventoryAction;

    [Header("References")]
    [SerializeField] private GameObject inventoryCanvas;
    [SerializeField] private ItemGrid boatItemGrid;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform canvasTransform;

    #endregion

    #region Private Fields

    private ItemGrid selectedItemGrid;
    private InventoryItem selectedItem;
    private InventoryItem overlapItem;
    private RectTransform heldItemRect;
    private InventotyHighlight inventoryHighlight;

    // Cache para optimización del highlight — solo recalculamos si cambia la celda o la rotación.
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

        // ForceInit garantiza que inventoryItemSlots esté listo aunque el Canvas esté desactivado.
        // Sin esto, TransferDiverLoot falla porque Start() nunca corre en objetos desactivados.
        boatItemGrid?.ForceInit();

        if (inventoryCanvas != null)
            inventoryCanvas.SetActive(false);

        ValidateReferences();
    }

    private void OnEnable() => RegisterInputs();
    private void OnDisable() => UnregisterInputs();

    private void Update()
    {
        // El item en mano sigue al cursor cada frame.
        if (selectedItem != null && heldItemRect != null)
            heldItemRect.position = GetPointerPosition();

        // Usamos Mouse.current en vez de InputAction para los clics en el Canvas.
        // Esto evita conflictos entre el New Input System y el sistema de Raycast de la UI de Unity.
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

        // Toggle siempre activo — GameController gestiona qué estados permiten abrirlo.
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
            // No desactivamos toggleInventoryAction — pertenece al mapa Global
            // que GameController gestiona. Desactivarlo aquí causaría que no se pueda reabrir.
        }
    }

    #endregion

    #region Input Callbacks

    private void OnRotatePerformed(InputAction.CallbackContext ctx)
    {
        if (selectedItem == null) return;
        selectedItem.Rotate();
        // Invalidamos cache para que HandleHighlight recalcule aunque el cursor no se mueva.
        lastRotationIdx = -1;
    }

    private void OnToggleInventoryPerformed(InputAction.CallbackContext ctx)
    {
        // Delegamos en GameController para que gestione el estado global del juego.
        if (GameController.Instance != null)
            GameController.Instance.ToggleInventory();
        else
            Debug.LogWarning("[InventoryController] GameController.Instance es null");
    }

    #endregion

    #region Public API

    // GameController llama esto. visible=true también dispara la transferencia del botín.
    public void SetInventoryVisible(bool visible)
    {
        if (inventoryCanvas == null) return;

        inventoryCanvas.SetActive(visible);

        if (visible)
        {
            // Al abrir, transferimos automáticamente lo que el buzo haya recogido.
            if (InventoryManager.Instance != null)
                TransferDiverLoot(InventoryManager.Instance.GetDiverInventory());
        }
        else
        {
            // Si cerramos con algo en la mano, lo destruimos para no dejar estado sucio.
            if (selectedItem != null)
            {
                Destroy(selectedItem.gameObject);
                selectedItem = null;
                heldItemRect = null;
            }
        }
    }

    // Lee el DiverInventory e instancia un InventoryItem por cada ItemData,
    // colocándolo automáticamente en el grid del barco.
    // Los items que no quepan se quedan en el DiverInventory para la próxima vez.
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

            // Buscamos hueco directamente en boatItemGrid sin depender de selectedItemGrid.
            // La transferencia es una operación del sistema, no del jugador.
            Vector2Int? slot = boatItemGrid.FindSpaceForObject(newItem);

            if (slot == null)
            {
                Debug.LogWarning("[InventoryController] Sin espacio para: " + loot.name);
                Destroy(newItem.gameObject);
                continue;
            }

            boatItemGrid.PlaceItem(newItem, slot.Value.x, slot.Value.y);
            transferidos.Add(loot);
            Debug.Log("[InventoryController] Transferido al barco: " + loot.name);
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
    }

    private void PlaceItem(Vector2Int tile)
    {
        if (selectedItem == null || selectedItemGrid == null) return;

        bool placed = selectedItemGrid.PlaceItem(selectedItem, tile.x, tile.y, ref overlapItem);
        if (!placed) return;

        selectedItem = null;
        heldItemRect = null;

        // Swap estilo RE4: si había un item, lo recogemos automáticamente.
        if (overlapItem != null)
        {
            selectedItem = overlapItem;
            overlapItem = null;
            heldItemRect = selectedItem.GetComponent<RectTransform>();
            heldItemRect?.SetAsLastSibling();
        }
    }

    #endregion

    #region Highlight

    private void HandleHighlight()
    {
        if (inventoryHighlight == null || selectedItemGrid == null) return;

        Vector2Int currentPos = GetTileGridPosition();
        int currentRot = selectedItem != null ? selectedItem.RotationIndex : -1;

        // Solo recalculamos si la celda cambió o el item rotó.
        // Esto evita trabajo innecesario 60 veces por segundo.
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
        if (toggleInventoryAction == null) Debug.LogWarning("[InventoryController] toggleInventoryAction no asignado");
        if (pointerPositionAction == null) Debug.LogWarning("[InventoryController] pointerPositionAction no asignado");
    }

    #endregion
}