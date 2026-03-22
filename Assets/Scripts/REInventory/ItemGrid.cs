using System;
using UnityEngine;


// Maneja la lógica del grid del inventario.
// Almacena items en un array 2D y gestiona operaciones de colocación, recogida y validación.
public class ItemGrid : MonoBehaviour
{
    #region Constants

    
    // Ancho de cada celda del grid en píxeles.
    
    public const float tileSizeWidht = 32f;

    
    // Alto de cada celda del grid en píxeles.
    
    public const float tileSizeHeight = 32f;

    #endregion

    #region Serialized Fields

    [Header("Grid Configuration")]
    [Tooltip("Ancho del grid en número de celdas")]
    [SerializeField] private int gridWidth = 20;

    [Tooltip("Alto del grid en número de celdas")]
    [SerializeField] private int gridHeight = 10;

    [Header("References (Optional)")]
    [Tooltip("Prefab del item de inventario (opcional, para debug/spawn)")]
    [SerializeField] private GameObject inventoryItemPrefab;

    #endregion

    #region Private Fields

  
    // Array 2D que almacena las referencias a items.
    // Cada celda puede estar vacía (null) o contener una referencia al item que la ocupa.
    
    private InventoryItem[,] inventoryItemSlots;

   
    // RectTransform de este grid (cache para performance).
  
    private RectTransform rectTransform;

  
    // Cache de Vector2 para evitar crear nuevos objetos en GetTileGridPosition.
    // Optimización de garbage collection.
   
    private Vector2 positionOnTheGrid = new Vector2();

    
    // Cache de Vector2Int para evitar crear nuevos objetos en GetTileGridPosition.
    // Optimización de garbage collection.
    
    private Vector2Int tileGridPosition = new Vector2Int();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("[ItemGrid] No se encontró RectTransform en " + gameObject.name);
            return;
        }

        Init(gridWidth, gridHeight);
    }

    #endregion

    #region Initialization


    // Inicializa el grid con las dimensiones especificadas.
    // Crea el array 2D y ajusta el tamaño del RectTransform.

    private void Init(int width, int height)
    {
        // Validar dimensiones
        if (width <= 0 || height <= 0)
        {
            Debug.LogError("[ItemGrid] Dimensiones inválidas: " + width + "x" + height);
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }

        // Crear array 2D
        inventoryItemSlots = new InventoryItem[width, height];

        // Ajustar tamaño visual del grid
        if (rectTransform != null)
        {
            Vector2 size = new Vector2(width * tileSizeWidht, height * tileSizeHeight);
            rectTransform.sizeDelta = size;
        }

        Debug.Log("[ItemGrid] Inicializado: " + width + "x" + height + " (" + (width * height) + " celdas)");
    }

    #endregion

    #region Position Conversion

   
    // Convierte una posición del ratón en pantalla a coordenadas de grid.
    // Utiliza caches para evitar crear nuevos objetos (optimización).
   
    public Vector2Int GetTileGridPosition(Vector2 mousePosition)
    {
        if (rectTransform == null)
        {
            Debug.LogWarning("[ItemGrid] rectTransform es null en GetTileGridPosition");
            return Vector2Int.zero;
        }

        // Calcular posición relativa al grid
        positionOnTheGrid.x = mousePosition.x - rectTransform.position.x;
        positionOnTheGrid.y = rectTransform.position.y - mousePosition.y;

        // Convertir a coordenadas de celda
        tileGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidht);
        tileGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight);

        return tileGridPosition;
    }

   
    // Nombre alternativo para compatibilidad con código antiguo.
   
  
    [Obsolete("Usar GetTileGridPosition en su lugar (typo corregido)")]
    public Vector2Int GetTitleGridPosiiton(Vector2 mousePosition)
    {
        return GetTileGridPosition(mousePosition);
    }

   
    // Calcula la posición local (dentro del grid) donde debe dibujarse un item.
    // El item se centra en su área ocupada.
    
    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning("[ItemGrid] InventoryItem es null en CalculatePositionOnGrid");
            return Vector2.zero;
        }

        Vector2 position = new Vector2();

        // Centrar el item en su área ocupada
        position.x = posX * tileSizeWidht + tileSizeWidht * inventoryItem.WIDTH / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.HEIGHT / 2);

        return position;
    }

    #endregion

    #region Item Placement
    // Permite forzar la inicialización desde fuera cuando el grid
    // está en un Canvas desactivado y Start no ha corrido todavía.
    public void ForceInit()
    {
        if (inventoryItemSlots != null) return; // Ya inicializado

        rectTransform = GetComponent<RectTransform>();
        Init(gridWidth, gridHeight);
    }

    // Intenta colocar un item en el grid en la posición especificada.
    // Valida límites, solapamientos y maneja el swap de items si es necesario.

    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning("[ItemGrid] Intentando colocar un item null");
            return false;
        }

        // Validar que el item cabe dentro de los límites del grid
        if (!BoundaryCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT))
        {
            return false;
        }

        // Validar solapamientos (puede haber un item para hacer swap)
        if (!OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem))
        {
            overlapItem = null;
            return false;
        }

        // Si hay un item en esa posición, limpiarlo del grid (para hacer swap)
        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        // Colocar el item
        PlaceItem(inventoryItem, posX, posY);

        return true;
    }

   
    // Coloca un item en el grid sin validaciones previas.
    //  Solo llamar después de validar con PlaceItem(item, x, y, ref overlap).
   
    public void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning("[ItemGrid] Intentando colocar un item null en PlaceItem interno");
            return;
        }

        // Emparentar el item al grid
        RectTransform itemRect = inventoryItem.GetComponent<RectTransform>();

        if (itemRect == null)
        {
            Debug.LogError("[ItemGrid] El InventoryItem no tiene RectTransform");
            return;
        }

        if (rectTransform != null)
        {
            itemRect.SetParent(rectTransform);
        }

        // Marcar todas las celdas que ocupa el item
        for (int i = 0; i < inventoryItem.WIDTH; i++)
        {
            for (int j = 0; j < inventoryItem.HEIGHT; j++)
            {
                int x = posX + i;
                int y = posY + j;

                // Validar límites del array para evitar IndexOutOfRangeException
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    inventoryItemSlots[x, y] = inventoryItem;
                }
                else
                {
                    Debug.LogError("[ItemGrid] Intentando escribir fuera de límites: (" + x + ", " + y + ")");
                }
            }
        }

        // Guardar posición en el item
        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;

        // Calcular y aplicar posición visual
        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);
        itemRect.localPosition = position;
    }

    #endregion

    #region Item Removal

   
    // Recoge un item de una posición específica del grid.
   
    public InventoryItem PickUpItem(int x, int y)
    {
        // Validar límites
        if (!PositionCheck(x, y))
        {
            Debug.LogWarning("[ItemGrid] PickUpItem fuera de límites: (" + x + ", " + y + ")");
            return null;
        }

        InventoryItem toReturn = inventoryItemSlots[x, y];

        if (toReturn == null)
        {
            return null;
        }

        // Limpiar todas las referencias del item en el grid
        CleanGridReference(toReturn);

        return toReturn;
    }

   
    // Limpia todas las referencias de un item del grid.
    // Recorre todas las celdas que ocupa y las pone a null.
  
    private void CleanGridReference(InventoryItem item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemGrid] Intentando limpiar un item null");
            return;
        }

        for (int i = 0; i < item.WIDTH; i++)
        {
            for (int j = 0; j < item.HEIGHT; j++)
            {
                int x = item.onGridPositionX + i;
                int y = item.onGridPositionY + j;

                // Validar límites antes de escribir
                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    inventoryItemSlots[x, y] = null;
                }
                else
                {
                    Debug.LogWarning("[ItemGrid] CleanGridReference fuera de límites: (" + x + ", " + y + ")");
                }
            }
        }
    }

    #endregion

    #region Validation & Queries

   
    // Verifica si una posición está dentro de los límites del grid.
    
    private bool PositionCheck(int posX, int posY)
    {
        if (posX < 0 || posY < 0)
        {
            return false;
        }

        if (posX >= gridWidth || posY >= gridHeight)
        {
            return false;
        }

        return true;
    }

    
    // Verifica si un área rectangular cabe dentro de los límites del grid.
    // Valida tanto la esquina superior izquierda como la inferior derecha.
    
    public bool BoundaryCheck(int posX, int posY, int width, int height)
    {
        // Validar esquina superior izquierda
        if (!PositionCheck(posX, posY))
        {
            return false;
        }

        // Validar esquina inferior derecha
        int bottomRightX = posX + width - 1;
        int bottomRightY = posY + height - 1;

        if (!PositionCheck(bottomRightX, bottomRightY))
        {
            return false;
        }

        return true;
    }

 
    
   
    [Obsolete("Usar BoundaryCheck en su lugar (typo corregido)")]
    public bool BoundyCheck(int posX, int posY, int width, int height)
    {
        return BoundaryCheck(posX, posY, width, height);
    }

   
    // Verifica si hay solapamiento al intentar colocar un item.
    // Permite hacer swap si todas las celdas ocupadas pertenecen al mismo item.
  
    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int x = posX + i;
                int y = posY + j;

                // Validar límites (extra safety)
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    continue;
                }

                InventoryItem itemInSlot = inventoryItemSlots[x, y];

                if (itemInSlot != null)
                {
                    // Primera celda ocupada encontrada
                    if (overlapItem == null)
                    {
                        overlapItem = itemInSlot;
                    }
                    // Hay múltiples items diferentes → no se puede colocar
                    else if (overlapItem != itemInSlot)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

   
    // Verifica si un área está completamente vacía (sin items).
    // </summary>
    
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                int x = posX + i;
                int y = posY + j;

                // Validar límites
                if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
                {
                    return false;
                }

                if (inventoryItemSlots[x, y] != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

   
    // Obtiene el item en una posición específica del grid.
   
    public InventoryItem GetItem(int x, int y)
    {
        // Validar límites
        if (!PositionCheck(x, y))
        {
            return null;
        }

        return inventoryItemSlots[x, y];
    }

   
    // Busca automáticamente un espacio libre en el grid para un item.
    // Recorre el grid de izquierda a derecha, arriba a abajo.
  
    public Vector2Int? FindSpaceForObject(InventoryItem itemToInsert)
    {
        if (itemToInsert == null)
        {
            Debug.LogWarning("[ItemGrid] itemToInsert es null en FindSpaceForObject");
            return null;
        }

        // Calcular límites de búsqueda
        int maxY = gridHeight - itemToInsert.HEIGHT + 1;
        int maxX = gridWidth - itemToInsert.WIDTH + 1;

        // Validar que el item no sea más grande que el grid
        if (maxY <= 0 || maxX <= 0)
        {
            Debug.LogWarning("[ItemGrid] Item demasiado grande para el grid: " +
                itemToInsert.WIDTH + "x" + itemToInsert.HEIGHT +
                " vs grid " + gridWidth + "x" + gridHeight);
            return null;
        }

        // Buscar primera posición disponible
        for (int y = 0; y < maxY; y++)
        {
            for (int x = 0; x < maxX; x++)
            {
                if (CheckAvailableSpace(x, y, itemToInsert.WIDTH, itemToInsert.HEIGHT))
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return null;
    }

   
  
    [Obsolete("Usar FindSpaceForObject en su lugar (typo corregido)")]
    public Vector2Int? FindSpaceForObeject(InventoryItem itemToInsert)
    {
        return FindSpaceForObject(itemToInsert);
    }

    #endregion

    #region Public Getters

   
    // Obtiene las dimensiones del grid.
   
    public Vector2Int GetGridSize()
    {
        return new Vector2Int(gridWidth, gridHeight);
    }

   
    /// Obtiene el ancho del grid.
   
    public int GetWidth() => gridWidth;

   
    // Obtiene el alto del grid.
    
    public int GetHeight() => gridHeight;

   
    // Obtiene el número total de celdas en el grid.
   
    public int GetTotalCells() => gridWidth * gridHeight;

    #endregion

    #region Debug Helpers


    
    // Valida la configuración en el editor.
   
    private void OnValidate()
    {
        if (gridWidth <= 0)
        {
            Debug.LogWarning("[ItemGrid] gridWidth debe ser mayor que 0");
            gridWidth = 1;
        }

        if (gridHeight <= 0)
        {
            Debug.LogWarning("[ItemGrid] gridHeight debe ser mayor que 0");
            gridHeight = 1;
        }
    }

  
    // Dibuja el grid en el editor para visualización.
    
    private void OnDrawGizmosSelected()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (rectTransform == null) return;

        Gizmos.color = Color.cyan;

        // Dibujar borde del grid
        Vector3 center = rectTransform.position;
        Vector3 size = new Vector3(gridWidth * tileSizeWidht, gridHeight * tileSizeHeight, 0f);
        Gizmos.DrawWireCube(center, size);

        // Dibujar líneas de celdas
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);

        // Líneas verticales
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = center + new Vector3(
                -size.x / 2 + x * tileSizeWidht,
                size.y / 2,
                0f
            );
            Vector3 end = center + new Vector3(
                -size.x / 2 + x * tileSizeWidht,
                -size.y / 2,
                0f
            );
            Gizmos.DrawLine(start, end);
        }

        // Líneas horizontales
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = center + new Vector3(
                -size.x / 2,
                size.y / 2 - y * tileSizeHeight,
                0f
            );
            Vector3 end = center + new Vector3(
                size.x / 2,
                size.y / 2 - y * tileSizeHeight,
                0f
            );
            Gizmos.DrawLine(start, end);
        }
    }


    #endregion
}