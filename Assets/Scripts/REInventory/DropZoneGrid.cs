using UnityEngine;
using System.Collections.Generic;

// Grid de descarte: los items colocados aquí se "tiran al mar" al cerrar el inventario.
// En vez de destruirlos sin más, re-instancia el prefab del mundo (con Rigidbody2D)
// en el punto de spawn del barco — el objeto cae al agua por física.
[RequireComponent(typeof(ItemGrid))]
public class DropZoneGrid : MonoBehaviour
{
    #region Serialized Fields

    [Header("Spawn del mundo")]
    [Tooltip("Transform desde donde caerán los objetos descartados (p.ej. el borde del barco). " +
             "Debe estar en coordenadas del mundo 2D, no del Canvas.")]
    [SerializeField] private Transform worldSpawnPoint;

    [Tooltip("Offset aleatorio en X para que los objetos no caigan todos apilados.")]
    [SerializeField] private float spawnScatterRadius = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Image del fondo del grid (opcional) — cambia de color si hay items dentro.")]
    [SerializeField] private UnityEngine.UI.Image backgroundImage;

    [SerializeField] private Color emptyColor = new Color(0.8f, 0.3f, 0.3f, 0.3f);
    [SerializeField] private Color occupiedColor = new Color(0.9f, 0.2f, 0.2f, 0.55f);

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    #endregion

    #region Private Fields

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
            Debug.LogWarning("[DropZoneGrid] worldSpawnPoint no asignado — los items descartados " +
                             "no podrán re-instanciarse en el mundo.");

        UpdateVisualFeedback();
    }

    #endregion

    #region Public API

    // Llamado por InventoryController al cerrar el inventario.
    // Para cada item del drop zone:
    //   1. Lo quita del grid (limpia el array interno).
    //   2. Re-instancia su worldPrefab en el spawn point del barco.
    //      El Rigidbody2D lo hará caer al agua automáticamente.
    //   3. Destruye el InventoryItem (el sprite de UI).
    public void DiscardAllItems()
    {
        List<InventoryItem> items = CollectUniqueItems();

        if (items.Count == 0)
        {
            LogDebug("Drop zone vacío, nada que descartar.");
            return;
        }

        LogDebug($"Descartando {items.Count} items — re-instanciando en el mundo...");

        foreach (InventoryItem item in items)
        {
            if (item == null) continue;

            // 1. Limpiar referencia del grid.
            itemGrid.PickUpItem(item.onGridPositionX, item.onGridPositionY);

            // 2. Re-instanciar el prefab del mundo si está configurado.
            SpawnWorldObject(item.itemData);

            // 3. Destruir el sprite de UI.
            LogDebug($"  → Descartado: {(item.itemData != null ? item.itemData.itemName : item.name)}");
            Destroy(item.gameObject);
        }

        UpdateVisualFeedback();
        LogDebug("Descarte completado.");
    }

    // Llamado por InventoryController cada vez que se coloca/recoge un item
    // para que el fondo del drop zone cambie de color.
    public void RefreshVisual()
    {
        UpdateVisualFeedback();
    }

    #endregion

    #region Private — Spawn

    // Re-instancia el prefab del mundo en la posición del barco con un scatter aleatorio.
    // El Rigidbody2D (Dynamic, Gravity Scale > 0) se encarga de hacerlo caer.
    private void SpawnWorldObject(ItemData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[DropZoneGrid] ItemData null — no se puede re-instanciar.");
            return;
        }

        if (data.worldPrefab == null)
        {
            Debug.LogWarning($"[DropZoneGrid] '{data.itemName}' no tiene worldPrefab asignado. " +
                             "Asígnalo en el ScriptableObject para que reaparezca en el mundo.");
            return;
        }

        if (worldSpawnPoint == null)
        {
            Debug.LogWarning("[DropZoneGrid] worldSpawnPoint no asignado. " +
                             "El objeto no puede re-instanciarse sin saber dónde.");
            return;
        }

        // Posición base del spawn con scatter aleatorio en X
        // para que los objetos no caigan todos exactamente en el mismo punto.
        Vector3 spawnPos = worldSpawnPoint.position;
        spawnPos.x += Random.Range(-spawnScatterRadius, spawnScatterRadius);

        GameObject worldObj = Instantiate(data.worldPrefab, spawnPos, Quaternion.identity);

        // Opcional: si el Rigidbody2D tiene velocidad inicial 0,
        // le damos un pequeño impulso hacia abajo para que no flote.
        Rigidbody2D rb = worldObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Gravity Scale ya lo hará caer; esto solo añade un empujón inicial.
            rb.linearVelocity = new Vector2(
                Random.Range(-0.5f, 0.5f),  // pequeña deriva lateral
                -1f                          // empuja ligeramente hacia abajo
            );
        }

        LogDebug($"  → Spawneado en mundo: {data.itemName} en {spawnPos}");
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