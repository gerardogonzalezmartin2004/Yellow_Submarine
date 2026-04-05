using UnityEngine;

// Memento Pattern: Guarda un snapshot del estado de un InventoryItem.
// Permite restaurar la posición, rotación y grid padre del item.
public class ItemMemento
{
    #region Memento Data

    // Grid donde estaba colocado el item.
    public ItemGrid SourceGrid { get; private set; }

    // Posición X en el grid.
    public int GridX { get; private set; }

    // Posición Y en el grid.
    public int GridY { get; private set; }

    // Índice de rotación (0-3).
    public int RotationIndex { get; private set; }

    // Timestamp de cuándo se creó este memento
    public float Timestamp { get; private set; }

    // Indica si este memento tiene datos válidos.
    // Un memento sin grid es inválido (item nunca estuvo colocado).
    public bool IsValid => SourceGrid != null;

    #endregion

    #region Constructors

    // Constructor privado. Usar CreateMemento() para crear instancias.
    private ItemMemento() { }

    // Crea un memento capturando el estado actual del item.
    public static ItemMemento CreateMemento(InventoryItem item, ItemGrid grid)
    {
        if (item == null)
        {
            Debug.LogWarning("[ItemMemento] Intentando crear memento de item null");
            return CreateInvalidMemento();
        }

        if (grid == null)
        {
            Debug.LogWarning("[ItemMemento] Intentando crear memento con grid null");
            return CreateInvalidMemento();
        }

        return new ItemMemento
        {
            SourceGrid = grid,
            GridX = item.onGridPositionX,
            GridY = item.onGridPositionY,
            RotationIndex = item.RotationIndex,
            Timestamp = Time.time
        };
    }

    // Crea un memento inválido (para items que nunca estuvieron colocados).
    public static ItemMemento CreateInvalidMemento()
    {
        return new ItemMemento
        {
            SourceGrid = null,
            GridX = -1,
            GridY = -1,
            RotationIndex = 0,
            Timestamp = Time.time
        };
    }

    #endregion

    #region Public Methods

    // Restaura el item a la posición guardada en este memento.
    // Retorna true si la restauración fue exitosa.
    public bool RestoreItem(InventoryItem item)
    {
        if (!IsValid)
        {
            Debug.LogWarning("[ItemMemento] Intentando restaurar desde memento inválido");
            return false;
        }

        if (item == null)
        {
            Debug.LogError("[ItemMemento] Item es null en RestoreItem");
            return false;
        }

        if (SourceGrid == null)
        {
            Debug.LogError("[ItemMemento] SourceGrid es null (memento corrupto)");
            return false;
        }

        // Restaurar rotación
        while (item.RotationIndex != RotationIndex)
        {
            item.Rotate();
        }

        // Intentar colocar en la posición original
        InventoryItem overlap = null;
        bool placed = SourceGrid.PlaceItem(item, GridX, GridY, ref overlap);

        if (placed)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Comprueba si este memento apunta al mismo lugar que otro.
    public bool IsSameLocation(ItemMemento other)
    {
        if (other == null) return false;
        return SourceGrid == other.SourceGrid &&
               GridX == other.GridX &&
               GridY == other.GridY;
    }

    #endregion

    
}