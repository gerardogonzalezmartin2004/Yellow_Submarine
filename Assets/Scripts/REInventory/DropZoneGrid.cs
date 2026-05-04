using UnityEngine;
using System.Collections.Generic;

// Grid de descarte: los items colocados aquí se "tiran al mar" al cerrar el inventario.
// No instancia prefabs nuevos — reactiva el GameObject original del mundo
// (que fue desactivado por LootPickup al recogerlo) y lo teletransporta al spawn point.
// El Rigidbody2D lo hace caer al agua por física.
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

    // Llamado por InventoryController al cerrar el inventario.
    // Para cada InventoryItem en el drop zone:
    //   1. Limpia el array del grid.
    //   2. Si tiene worldObject → lo teletransporta al spawn, lo reactiva y le da impulso.
    //   3. Destruye el sprite de UI.
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

            // 1. Limpiar el grid.
            itemGrid.PickUpItem(item.onGridPositionX, item.onGridPositionY);

            // 2. Reactivar el objeto del mundo.
            RestoreWorldObject(item);

            // 3. Destruir el sprite de UI.
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
            // Reactivar en su posición actual como fallback.
            worldObj.SetActive(true);
            return;
        }

        // Teletransportar al punto de spawn con scatter aleatorio en X.
        Vector3 spawnPos = worldSpawnPoint.position;
        spawnPos.x += Random.Range(-spawnScatterRadius, spawnScatterRadius);
        worldObj.transform.position = spawnPos;

        // Resetear el flag de LootPickup para que el buzo pueda volver a recogerlo.
        LootPickup lootPickup = worldObj.GetComponent<LootPickup>();
        lootPickup?.ResetForReuse();

        // Resetear velocidad del Rigidbody para que caiga limpio.
        Rigidbody2D rb = worldObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                Random.Range(-0.3f, 0.3f),  // deriva lateral leve
                -0.5f                        // pequeño empujón hacia abajo
            );
            rb.angularVelocity = 0f;
        }

        // ── REACTIVAR: el objeto vuelve a ser visible y colisionable.
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