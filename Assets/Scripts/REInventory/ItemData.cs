using UnityEngine;

// ScriptableObject que almacena los datos de un item del inventario.
// Define el tamaño (width x height) y el sprite visual.

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data", order = 1)]
public class ItemData : ScriptableObject
{
    #region Serialized Fields

    [Header("Tamaño del Item")]
    [Tooltip("Ancho del item en celdas del grid")]
    [Min(1)]
    public int width = 1;

    [Tooltip("Alto del item en celdas del grid")]
    [Min(1)]
    public int height = 1;

    [Header("Visual")]
    [Tooltip("Sprite que se mostrará en el inventario")]
    public Sprite itemIcon;

    #endregion

    #region Validation

#if UNITY_EDITOR
   
    // Valida los datos en el editor para evitar configuraciones inválidas.
   
    private void OnValidate()
    {
        // Asegurar que width y height sean al menos 1
        if (width < 1)
        {
            Debug.LogWarning("[ItemData] Width no puede ser menor que 1 en " + name);
            width = 1;
        }

        if (height < 1)
        {
            Debug.LogWarning("[ItemData] Height no puede ser menor que 1 en " + name);
            height = 1;
        }

        // Warning si falta el icono
        if (itemIcon == null)
        {
            Debug.LogWarning("[ItemData] Falta asignar itemIcon en " + name);
        }
    }
#endif

    #endregion

    #region Public Helpers

    
    // Obtiene el área total del item (width * height).
    // Útil para comparar tamaños o calcular valor.
    
    public int GetArea()
    {
        return width * height;
    }

   
    // Verifica si el item es cuadrado (width == height).
    // Los items cuadrados no cambian de forma al rotar.
    
    public bool IsSquare()
    {
        return width == height;
    }

    #endregion
}