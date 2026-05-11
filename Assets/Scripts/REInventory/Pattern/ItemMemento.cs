using UnityEngine;


public class ItemMemento
{
    #region Memento Data

    
    public ItemGrid SourceGrid { get; private set; }

    public int GridX { get; private set; }

    
    public int GridY { get; private set; }

   
    public int RotationIndex { get; private set; }

    
    public float Timestamp { get; private set; }

    
    public bool IsValid => SourceGrid != null;

    #endregion

    #region Constructors

   
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

        while (item.RotationIndex != RotationIndex)
        {
            item.Rotate();
        }

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

    public bool IsSameLocation(ItemMemento other)
    {
        if (other == null) return false;
        return SourceGrid == other.SourceGrid &&
               GridX == other.GridX &&
               GridY == other.GridY;
    }

    #endregion


}