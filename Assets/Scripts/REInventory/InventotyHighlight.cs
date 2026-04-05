using UnityEngine;


// Maneja el highlight visual que muestra dónde caerá un item en el grid.
// El recuadro se dibuja por encima de las celdas pero por debajo del item arrastrado.

public class InventotyHighlight : MonoBehaviour
{
    #region Serialized Fields

    [Header("Referencias")]
    [Tooltip("RectTransform del recuadro de highlight")]
    [SerializeField] private RectTransform highlighter;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Validar que tenemos la referencia
        if (highlighter == null)
        {
            Debug.LogError("[InventotyHighlight] highlighter no asignado en " + gameObject.name);
        }
    }

    #endregion

    #region Public Methods

   
    // Muestra u oculta el highlight.
   
    public void Show(bool show)
    {
        if (highlighter == null)
        {
            return;
        }

        highlighter.gameObject.SetActive(show);
    }

  
    // Ajusta el tamaño del highlight para que coincida con el item objetivo.
   
    public void SetSize(InventoryItem targetItem)
    {
        if (highlighter == null || targetItem == null)
        {
            return;
        }

        // Calcular tamaño en píxeles basándose en el tamaño del item (considerando rotación)
        Vector2 size = new Vector2
        {
            x = targetItem.WIDTH * ItemGrid.tileSizeWidht,
            y = targetItem.HEIGHT * ItemGrid.tileSizeHeight
        };

        highlighter.sizeDelta = size;
    }

 
    // Posiciona el highlight en la posición del item en el grid.
    // Usado cuando el item ya está colocado (hover sobre un item existente).
  
    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem)
    {
        if (highlighter == null || targetGrid == null || targetItem == null)
        {
            return;
        }

        // Calcular posición usando las coordenadas del item en el grid
        Vector2 pos = targetGrid.CalculatePositionOnGrid(
            targetItem,
            targetItem.onGridPositionX,
            targetItem.onGridPositionY
        );

        highlighter.localPosition = pos;
    }

   
    // Posiciona el highlight en una posición específica del grid.
   // Usado cuando el jugador está arrastrando un item (muestra dónde caería).
    
    public void SetPosition(ItemGrid targetGrid, InventoryItem targetItem, int posX, int posY)
    {
        if (highlighter == null || targetGrid == null || targetItem == null)
        {
            return;
        }

        // Calcular posición usando las coordenadas especificadas
        Vector2 pos = targetGrid.CalculatePositionOnGrid(targetItem, posX, posY);

        highlighter.localPosition = pos;
    }

   
    // Cambia el parent del highlight al grid especificado.
    //  Llama a SetAsLastSibling() para garantizar que se dibuje
    // por encima de las celdas pero por debajo del item arrastrado.
   
    public void SetParent(ItemGrid targetGrid)
    {
        if (highlighter == null)
        {
            return;
        }

        if (targetGrid == null)
        {
            // Si no hay grid, desemparentar
            highlighter.SetParent(null);
            return;
        }

        // Emparentar al RectTransform del grid
        RectTransform gridRect = targetGrid.GetComponent<RectTransform>();

        if (gridRect == null)
        {
            Debug.LogError("[InventotyHighlight] El ItemGrid no tiene RectTransform");
            return;
        }

        highlighter.SetParent(gridRect);

        // Asegurar que se dibuje por encima de las celdas
        // pero por debajo del item arrastrado
        highlighter.SetAsLastSibling();

        // Reset de la posición local al cambiar de parent
        highlighter.localPosition = Vector3.zero;
    }

    #endregion

    #region Public Helpers

    
    // Obtiene el color actual del highlight.
    // Útil para debugging o para cambiar el color según el estado.
    
    public Color GetColor()
    {
        if (highlighter == null)
        {
            return Color.white;
        }

        UnityEngine.UI.Image image = highlighter.GetComponent<UnityEngine.UI.Image>();

        if (image != null)
        {
            return image.color;
        }

        return Color.white;
    }

   
    // Cambia el color del highlight.
    // Útil para mostrar diferentes estados (válido = verde, inválido = rojo).
   
    public void SetColor(Color color)
    {
        if (highlighter == null)
        {
            return;
        }

        UnityEngine.UI.Image image = highlighter.GetComponent<UnityEngine.UI.Image>();

        if (image != null)
        {
            image.color = color;
        }
    }

    #endregion

    #region Debug Helpers


   
    // Valida la configuración en el editor.
   
    private void OnValidate()
    {
        if (highlighter == null)
        {
            Debug.LogWarning("[InventotyHighlight] Falta asignar highlighter en " + gameObject.name);
        }
    }


    #endregion
}