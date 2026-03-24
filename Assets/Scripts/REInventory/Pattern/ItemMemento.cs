using UnityEngine;

/// <summary>
/// Memento Pattern: Guarda un snapshot del estado de un InventoryItem.
/// Permite restaurar la posición, rotación y grid padre del item.
/// </summary>
public class ItemMemento
{
    #region Memento Data

    /// <summary>
    /// Grid donde estaba colocado el item.
    /// </summary>
    public ItemGrid SourceGrid { get; private set; }

    /// <summary>
    /// Posición X en el grid.
    /// </summary>
    public int GridX { get; private set; }

    /// <summary>
    /// Posición Y en el grid.
    /// </summary>
    public int GridY { get; private set; }

    /// <summary>
    /// Índice de rotación (0-3).
    /// </summary>
    public int RotationIndex { get; private set; }

    /// <summary>
    /// Timestamp de cuándo se creó este memento (para debugging).
    /// </summary>
    public float Timestamp { get; private set; }

    /// <summary>
    /// Indica si este memento tiene datos válidos.
    /// Un memento sin grid es inválido (item nunca estuvo colocado).
    /// </summary>
    public bool IsValid => SourceGrid != null;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor privado. Usar CreateMemento() para crear instancias.
    /// </summary>
    private ItemMemento() { }

    /// <summary>
    /// Crea un memento capturando el estado actual del item.
    /// </summary>
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

    /// <summary>
    /// Crea un memento inválido (para items que nunca estuvieron colocados).
    /// </summary>
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

    /// <summary>
    /// Restaura el item a la posición guardada en este memento.
    /// Retorna true si la restauración fue exitosa.
    /// </summary>
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
            Debug.Log($"[ItemMemento] Item restaurado a ({GridX}, {GridY}) en {SourceGrid.name}");
            return true;
        }
        else
        {
            Debug.LogWarning($"[ItemMemento] No se pudo restaurar item a ({GridX}, {GridY}) - espacio ocupado");
            return false;
        }
    }

    /// <summary>
    /// Comprueba si este memento apunta al mismo lugar que otro.
    /// </summary>
    public bool IsSameLocation(ItemMemento other)
    {
        if (other == null) return false;
        return SourceGrid == other.SourceGrid &&
               GridX == other.GridX &&
               GridY == other.GridY;
    }

    #endregion

    #region Debug

    public override string ToString()
    {
        if (!IsValid) return "[ItemMemento: INVALID]";

        return $"[ItemMemento: Grid={SourceGrid?.name}, Pos=({GridX},{GridY}), Rot={RotationIndex}, Time={Timestamp:F2}]";
    }

    #endregion
}