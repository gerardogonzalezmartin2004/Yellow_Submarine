using System;
using UnityEngine;
using UnityEngine.UI;

// Representa un item individual en el inventario (UI).
// Ahora también guarda referencia al worldObject (el GameObject desactivado en escena)
// para que DropZoneGrid pueda reactivarlo al descartar.
public class InventoryItem : MonoBehaviour
{
    #region Serialized Fields

    [Header("Item Data")]
    public ItemData itemData;

    #endregion

    #region Runtime Fields

    // Referencia al GameObject del mundo desactivado.
    // Se asigna en InventoryController.TransferDiverLoot.
    // DropZoneGrid lo usa para reactivar el objeto al descartar.
    [HideInInspector] public GameObject worldObject;

    #endregion

    #region Private Fields

    private int rotationIndex = 0;
    private RectTransform cachedRectTransform;
    private Image cachedImage;

    #endregion

    #region Public Properties

    public int HEIGHT
    {
        get
        {
            if (itemData == null) return 1;
            return IsRotationIndexOdd() ? itemData.width : itemData.height;
        }
    }

    public int WIDTH
    {
        get
        {
            if (itemData == null) return 1;
            return IsRotationIndexOdd() ? itemData.height : itemData.width;
        }
    }

    public int onGridPositionX;
    public int onGridPositionY;
    public int RotationIndex => rotationIndex;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        cachedRectTransform = GetComponent<RectTransform>();
        cachedImage = GetComponent<Image>();

        if (cachedRectTransform == null)
            Debug.LogError("[InventoryItem] Falta RectTransform en " + gameObject.name);
        if (cachedImage == null)
            Debug.LogError("[InventoryItem] Falta Image component en " + gameObject.name);
    }

    #endregion

    #region Public Methods

    public void Rotate()
    {
        rotationIndex = (rotationIndex + 1) % 4;
        UpdateVisualRotation();
    }

    public void Set(ItemData data)
    {
        if (data == null)
        {
            Debug.LogError("[InventoryItem] Set con ItemData null");
            return;
        }

        itemData = data;

        if (cachedImage != null && itemData.itemIcon != null)
            cachedImage.sprite = itemData.itemIcon;
        else if (itemData.itemIcon == null)
            Debug.LogWarning("[InventoryItem] itemIcon es null en " + itemData.name);

        UpdateSize();
        UpdateVisualRotation();
    }

    #endregion

    #region Private Methods

    private void UpdateVisualRotation()
    {
        if (cachedRectTransform == null) return;
        cachedRectTransform.rotation = Quaternion.Euler(0, 0, -90f * rotationIndex);
    }

    private void UpdateSize()
    {
        if (cachedRectTransform == null || itemData == null) return;

        Vector2 size = new Vector2(
            WIDTH * ItemGrid.tileSizeWidht,
            HEIGHT * ItemGrid.tileSizeHeight
        );
        cachedRectTransform.sizeDelta = size;
    }

    private bool IsRotationIndexOdd() => rotationIndex % 2 != 0;

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (itemData == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(WIDTH * ItemGrid.tileSizeWidht, HEIGHT * ItemGrid.tileSizeHeight, 1f));
    }

    #endregion
}