using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


/// <summary>
/// Sistema de debugging visual para ItemGrid.
/// Muestra overlays, información de celdas, y estado del grid en tiempo real.
/// </summary>
[RequireComponent(typeof(ItemGrid))]
public class ItemGridDebugger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebug = true;

    [SerializeField] private bool showCellInfo = true;
    [SerializeField] private bool showOccupiedCells = true;
    [SerializeField] private bool showItemBounds = true;
    [SerializeField] private bool logPickupAttempts = true;

    [Header("Visual Settings")]
    [SerializeField] private Color occupiedCellColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Color emptyCellColor = new Color(0f, 1f, 0f, 0.1f);
    [SerializeField] private Color itemBoundsColor = Color.yellow;

    #endregion

    #region Private Fields

    private ItemGrid grid;
    private Canvas debugCanvas;
    private GameObject debugOverlay;
    private Dictionary<Vector2Int, GameObject> cellDebugObjects = new Dictionary<Vector2Int, GameObject>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        grid = GetComponent<ItemGrid>();

        if (enableDebug)
        {
            CreateDebugOverlay();
        }
    }

    private void Update()
    {
        if (!enableDebug) return;

        if (showOccupiedCells)
        {
            UpdateCellDebugDisplay();
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableDebug || grid == null) return;

        DrawGridGizmos();
    }

    #endregion

    #region Debug Overlay

    private void CreateDebugOverlay()
    {
        // Crear canvas para overlays de debug
        debugOverlay = new GameObject("GridDebugOverlay");
        debugOverlay.transform.SetParent(transform);

        RectTransform rect = debugOverlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        CanvasGroup canvasGroup = debugOverlay.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void UpdateCellDebugDisplay()
    {
        if (grid == null || debugOverlay == null) return;

        Vector2Int gridSize = grid.GetGridSize();

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                InventoryItem item = grid.GetItem(x, y);

                if (item != null)
                {
                    // Celda ocupada
                    EnsureCellDebugObject(pos, occupiedCellColor);
                }
                else
                {
                    // Celda vacía
                    if (cellDebugObjects.ContainsKey(pos))
                    {
                        Destroy(cellDebugObjects[pos]);
                        cellDebugObjects.Remove(pos);
                    }
                }
            }
        }
    }

    private void EnsureCellDebugObject(Vector2Int pos, Color color)
    {
        if (!cellDebugObjects.ContainsKey(pos))
        {
            GameObject cellObj = new GameObject($"Cell_{pos.x}_{pos.y}");
            cellObj.transform.SetParent(debugOverlay.transform);

            RectTransform rect = cellObj.AddComponent<RectTransform>();
            Image img = cellObj.AddComponent<Image>();
            img.color = color;

            // Posicionar
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(ItemGrid.tileSizeWidht, ItemGrid.tileSizeHeight);

            Vector2 localPos = new Vector2(
                pos.x * ItemGrid.tileSizeWidht,
                -(pos.y * ItemGrid.tileSizeHeight)
            );
            rect.anchoredPosition = localPos;

            cellDebugObjects[pos] = cellObj;
        }
    }

    #endregion

    #region Gizmos

    private void DrawGridGizmos()
    {
        if (!Application.isPlaying) return;

        Vector2Int gridSize = grid.GetGridSize();
        RectTransform rectTransform = grid.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Dibujar celdas ocupadas
        Gizmos.color = occupiedCellColor;

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                InventoryItem item = grid.GetItem(x, y);

                if (item != null && showItemBounds)
                {
                    Vector3 cellCenter = rectTransform.position + new Vector3(
                        (x + 0.5f) * ItemGrid.tileSizeWidht - (gridSize.x * ItemGrid.tileSizeWidht / 2f),
                        -((y + 0.5f) * ItemGrid.tileSizeHeight - (gridSize.y * ItemGrid.tileSizeHeight / 2f)),
                        0
                    );

                    Vector3 cellSize = new Vector3(
                        ItemGrid.tileSizeWidht * 0.9f,
                        ItemGrid.tileSizeHeight * 0.9f,
                        1f
                    );

                    Gizmos.DrawCube(cellCenter, cellSize);

                    // Dibujar referencia al item
                    if (item.onGridPositionX == x && item.onGridPositionY == y)
                    {
                        // Esta es la celda de origen del item
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawWireCube(cellCenter, cellSize * 1.1f);
                        Gizmos.color = occupiedCellColor;
                    }
                }
            }
        }
    }

    #endregion

    #region Public Debug Methods

    /// <summary>
    /// Imprime el estado completo del grid en la consola.
    /// </summary>
    public void DumpGridState()
    {
        if (grid == null)
        {
            Debug.LogError("[GridDebugger] Grid es null");
            return;
        }

        Vector2Int gridSize = grid.GetGridSize();
        Debug.Log("=== GRID STATE DUMP ===");
        Debug.Log($"Tamaño: {gridSize.x}x{gridSize.y}");

        Dictionary<InventoryItem, Vector2Int> itemOrigins = new Dictionary<InventoryItem, Vector2Int>();

        // Encontrar todos los items únicos y sus orígenes
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                InventoryItem item = grid.GetItem(x, y);

                if (item != null && !itemOrigins.ContainsKey(item))
                {
                    itemOrigins[item] = new Vector2Int(item.onGridPositionX, item.onGridPositionY);
                }
            }
        }

        Debug.Log($"Items únicos en grid: {itemOrigins.Count}");

        foreach (var kvp in itemOrigins)
        {
            InventoryItem item = kvp.Key;
            Vector2Int origin = kvp.Value;

            Debug.Log($"  - {item.itemData.name} @ ({origin.x},{origin.y}) [{item.WIDTH}x{item.HEIGHT}] Rot:{item.RotationIndex}");
        }

        Debug.Log("======================");
    }

    /// <summary>
    /// Muestra información de una celda específica.
    /// </summary>
    public void InspectCell(int x, int y)
    {
        if (grid == null) return;

        InventoryItem item = grid.GetItem(x, y);

        Debug.Log($"=== CELL INSPECTION ({x},{y}) ===");

        if (item == null)
        {
            Debug.Log("Celda VACÍA");
        }
        else
        {
            Debug.Log($"Celda OCUPADA por: {item.itemData.name}");
            Debug.Log($"  Origen del item: ({item.onGridPositionX},{item.onGridPositionY})");
            Debug.Log($"  Tamaño: {item.WIDTH}x{item.HEIGHT}");
            Debug.Log($"  Rotación: {item.RotationIndex}");
            Debug.Log($"  GameObject: {item.gameObject.name}");
        }

        Debug.Log("================================");
    }

    /// <summary>
    /// Valida la integridad del grid (detecta items corruptos).
    /// </summary>
    public bool ValidateGridIntegrity()
    {
        if (grid == null) return false;

        Vector2Int gridSize = grid.GetGridSize();
        bool isValid = true;

        Dictionary<InventoryItem, List<Vector2Int>> itemCells = new Dictionary<InventoryItem, List<Vector2Int>>();

        // Recopilar todas las celdas de cada item
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                InventoryItem item = grid.GetItem(x, y);

                if (item != null)
                {
                    if (!itemCells.ContainsKey(item))
                    {
                        itemCells[item] = new List<Vector2Int>();
                    }
                    itemCells[item].Add(new Vector2Int(x, y));
                }
            }
        }

        // Validar cada item
        foreach (var kvp in itemCells)
        {
            InventoryItem item = kvp.Key;
            List<Vector2Int> cells = kvp.Value;

            int expectedCells = item.WIDTH * item.HEIGHT;

            if (cells.Count != expectedCells)
            {
                Debug.LogError($"[GridDebugger] Item {item.itemData.name} tiene {cells.Count} celdas, esperadas {expectedCells}");
                isValid = false;
            }

            // Verificar que las celdas forman un rectángulo contiguo
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Vector2Int cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            if (width != item.WIDTH || height != item.HEIGHT)
            {
                Debug.LogError($"[GridDebugger] Item {item.itemData.name} tiene forma incorrecta: {width}x{height} vs {item.WIDTH}x{item.HEIGHT}");
                isValid = false;
            }
        }

        if (isValid)
        {
            Debug.Log("[GridDebugger] ✅ Grid integrity OK");
        }
        else
        {
            Debug.LogError("[GridDebugger] ❌ Grid integrity FAILED");
        }

        return isValid;
    }

    #endregion

    #region Context Menu (Editor Only)

#if UNITY_EDITOR
    [ContextMenu("Dump Grid State")]
    private void ContextDumpGridState()
    {
        DumpGridState();
    }

    [ContextMenu("Validate Grid Integrity")]
    private void ContextValidateIntegrity()
    {
        ValidateGridIntegrity();
    }

    [ContextMenu("Clear All Debug Overlays")]
    private void ContextClearOverlays()
    {
        foreach (var kvp in cellDebugObjects)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        cellDebugObjects.Clear();
    }
#endif

    #endregion
}