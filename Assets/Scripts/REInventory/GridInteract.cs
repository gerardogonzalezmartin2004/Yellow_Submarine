using UnityEngine;
using UnityEngine.EventSystems;


// Detecta cuándo el cursor entra/sale del área de un ItemGrid.
// Notifica al InventoryController para que sepa qué grid está activo.

[RequireComponent(typeof(ItemGrid))]
public class GridInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Private Fields


    // Referencia al controlador de inventario 

    private InventoryController inventoryController;


    // Referencia al ItemGrid de este GameObject.

    private ItemGrid itemGrid;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Buscar el InventoryController en la escena
        inventoryController = FindAnyObjectByType(typeof(InventoryController)) as InventoryController;

        if (inventoryController == null)
        {
            Debug.LogError("[GridInteract] No se encontró InventoryController en la escena");
        }

        // Obtener el ItemGrid de este GameObject
        itemGrid = GetComponent<ItemGrid>();

        if (itemGrid == null)
        {
            Debug.LogError("[GridInteract] No se encontró ItemGrid en " + gameObject.name);
        }
    }

    #endregion

    #region Event System Callbacks


    // Se llama cuando el cursor entra en el área de este grid.
    // Notifica al controller que este es el grid activo.

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (inventoryController != null && itemGrid != null)
        {
            inventoryController.SelectedItemGrid = itemGrid;
        }
    }


    // Se llama cuando el cursor sale del área de este grid.
    // Notifica al controller que ya no hay grid activo.

    public void OnPointerExit(PointerEventData eventData)
    {
        if (inventoryController != null)
        {
            inventoryController.SelectedItemGrid = null;
        }
    }

    #endregion
}