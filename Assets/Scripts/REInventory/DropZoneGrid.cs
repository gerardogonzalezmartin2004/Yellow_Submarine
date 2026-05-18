using UnityEngine;
using System.Collections.Generic;

// este script se encarga de gestionar la zona de descarte del inventario, permitiendo al jugador descartar los items arrastrándolos a esta zona. Al hacerlo, los objetos correspondientes en el mundo se reactivan y caen desde un punto definido (como el borde del barco). También proporciona feedback visual para indicar si la zona está vacía o no.


[RequireComponent(typeof(ItemGrid))]
public class DropZoneGrid : MonoBehaviour
{
    #region Serialized Fields

    [Header("Spawn del mundo")]
    [Tooltip("Punto desde donde caerán los objetos descartados (borde del barco, en coordenadas mundo 2D).")]
    [SerializeField] private Transform worldSpawnPoint;

    [Tooltip("Scatter aleatorio en X para que no caigan apilados.")]
    [SerializeField] private float spawnScatterRadius = 0.5f;

    [Header("Visual Feedback")]
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private Color emptyColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);
    [SerializeField] private Color occupiedColor = new Color(0.9f, 0.2f, 0.2f, 0.55f);

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Private

    private ItemGrid itemGrid;

    #endregion

    #region Properties

    public ItemGrid Grid => itemGrid;
    public bool IsEmpty => CollectUniqueItems().Count == 0;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        itemGrid = GetComponent<ItemGrid>();
        if (itemGrid == null)
            Debug.LogError("[DropZoneGrid] No se encontró ItemGrid en " + gameObject.name);

        if (worldSpawnPoint == null)
            Debug.LogWarning("[DropZoneGrid] worldSpawnPoint no asignado.");

        UpdateVisualFeedback();
    }

    #endregion

    #region Public API

  
   
    public void DiscardAllItems()
    {
        List<InventoryItem> items = CollectUniqueItems();

        if (items.Count == 0)
        {
            LogDebug("Drop zone vacío.");
            return;
        }

        LogDebug($"Descartando {items.Count} items...");

        foreach (InventoryItem item in items)
        {
            if (item == null) continue;

            itemGrid.PickUpItem(item.onGridPositionX, item.onGridPositionY);

            RestoreWorldObject(item);

            LogDebug($"  Descartado UI: {item.name}");
            Destroy(item.gameObject);
        }

        UpdateVisualFeedback();
        LogDebug("Descarte completado.");
    }

    public void RefreshVisual() => UpdateVisualFeedback();

    #endregion

    #region Private — Restore world object

    private void RestoreWorldObject(InventoryItem inventoryItem)
    {
        GameObject worldObj = inventoryItem.worldObject;

        if (worldObj == null)
        {
            Debug.LogWarning($"[DropZoneGrid] '{inventoryItem.name}' no tiene worldObject — " +
                             "asegúrate de asignarlo en TransferDiverLoot.");
            return;
        }

        if (worldSpawnPoint == null)
        {
            Debug.LogWarning("[DropZoneGrid] worldSpawnPoint no asignado — no se puede restaurar la posición.");
            worldObj.SetActive(true);
            return;
        }

        Vector3 spawnPos = worldSpawnPoint.position;
        spawnPos.x += Random.Range(-spawnScatterRadius, spawnScatterRadius);
        worldObj.transform.position = spawnPos;

        LootPickup lootPickup = worldObj.GetComponent<LootPickup>();
        lootPickup?.ResetForReuse();

        Rigidbody2D rb = worldObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                Random.Range(-0.3f, 0.3f),  
                -0.5f                      
            );
            rb.angularVelocity = 0f;
        }

      
        worldObj.SetActive(true);

        LogDebug($"  Restaurado en mundo: {worldObj.name} en {spawnPos}");
    }

    #endregion

    #region Private — Helpers

    private List<InventoryItem> CollectUniqueItems()
    {
        if (itemGrid == null) return new List<InventoryItem>();

        Vector2Int size = itemGrid.GetGridSize();
        HashSet<InventoryItem> seen = new HashSet<InventoryItem>();
        List<InventoryItem> result = new List<InventoryItem>();

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                InventoryItem item = itemGrid.GetItem(x, y);
                if (item != null && seen.Add(item))
                    result.Add(item);
            }
        }

        return result;
    }

    private void UpdateVisualFeedback()
    {
        if (backgroundImage == null) return;
        backgroundImage.color = IsEmpty ? emptyColor : occupiedColor;
    }

    private void LogDebug(string msg)
    {
        if (showDebugLogs)
            Debug.Log($"[DropZoneGrid] {msg}");
    }

    #endregion
}