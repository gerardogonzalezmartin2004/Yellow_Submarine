using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Este es el "Cerebro" del inventario. No almacena los datos de los objetos (eso
// lo hace el ItemGrid), sino que se encarga de las acciones del jugador: 
// recoger, soltar, rotar y calcular dónde debe dibujarse el cuadrado verde (Highlight).

public class InventoryController : MonoBehaviour
{
    #region Serialized Fields - Input Actions

    [Header("Input Actions (New Input System)")]
    [Tooltip("Acción para hacer clic/interactuar (recoger/soltar items)")]
    [SerializeField] private InputActionReference interactAction;

    [Tooltip("Acción para rotar el item seleccionado")]
    [SerializeField] private InputActionReference rotateAction;

    [Tooltip("Acción para spawnear un item aleatorio (debug)")]
    [SerializeField] private InputActionReference spawnRandomItemAction;

    [Tooltip("Acción para insertar un item aleatorio en el grid (debug)")]
    [SerializeField] private InputActionReference insertRandomItemAction;

    [Tooltip("Acción para obtener la posición del cursor/puntero")]
    [SerializeField] private InputActionReference pointerPositionAction;

    #endregion

    #region Serialized Fields - References

    [Header("References")]
    [Tooltip("Lista de ItemData para generar items aleatorios (debug)")]
    [SerializeField] private List<ItemData> items;

    [Tooltip("Prefab del InventoryItem a instanciar")]
    [SerializeField] private GameObject itemPrefab;

    [Tooltip("Transform del Canvas donde se instanciarán los items")]
    [SerializeField] private Transform canvasTransform;

    #endregion

    #region Private Fields

    // El tablero sobre el que estamos pasando el ratón actualmente.
    private ItemGrid selectedItemGrid;
    
    // Esta es la "Mano" del jugador. Si no es null, es que llevamos un objeto flotando en el cursor.
    private InventoryItem selectedItem;

    // Usado temporalmente cuando soltamos un objeto encima de otro (Swap).
    private InventoryItem overlapItem;

    // Cacheamos el RectTransform del objeto en mano para no hacer GetComponent en cada frame (Optimización).
    private RectTransform rectTransform;

    // El cuadradito verde que le dice al jugador dónde va a caer el objeto.
    private InventotyHighlight inventoryHighlight;

    // Calcular si un objeto cabe en el tablero requiere matemáticas. Para no fundir la CPU 
    // calculando 60 veces por segundo, guardamos la última posición y rotación. 
    // Solo recalculamos si alguna de las dos cambia.
    private Vector2Int lastHighlightPosition = new Vector2Int(-1, -1);
    private int lastRotationIndex = -1;


    // Si no tenemos nada en la mano, guardamos aquí el objeto al que estamos apuntando.
    private InventoryItem itemToHighlight;

    #endregion

    #region Properties

    // Esta propiedad es usada por el script "GridInteract" de las casillas.
    // Cuando el ratón entra en un tablero, ese tablero se "inyecta" a sí mismo aquí.
    public ItemGrid SelectedItemGrid
    {
        get => selectedItemGrid;
        set
        {
            selectedItemGrid = value;

            if (inventoryHighlight != null)
            {
                inventoryHighlight.SetParent(value);
            }
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Obtener referencia al highlight
        inventoryHighlight = GetComponent<InventotyHighlight>();

        if (inventoryHighlight == null)
        {
            Debug.LogError("[InventoryController] No se encontró InventotyHighlight en el mismo GameObject");
        }

        // Validar referencias
        ValidateReferences();
    }

    private void OnEnable()
    {
        // Activar las acciones de input
        EnableInputActions();
    }

    private void OnDisable()
    {
        // Desactivar las acciones de input
        DisableInputActions();
    }

    private void Update()
    {
        // Hacer que el item seleccionado siga al cursor
        UpdateSelectedItemPosition();

        // Procesar inputs
        HandleInputs();

        // Si no hay grid seleccionado, ocultar highlight y salir
        if (selectedItemGrid == null)
        {
            if (inventoryHighlight != null)
            {
                inventoryHighlight.Show(false);
            }
            return;
        }

        // Actualizar el highlight 
        HandleHighlight();
    }

    #endregion

    #region Input System Setup

    // El patrón es siempre el mismo: Activar la acción (Enable) y decirle qué función
    // ejecutar cuando el botón se pulse (+= performed).
    private void EnableInputActions()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPerformed;
        }

        if (rotateAction != null && rotateAction.action != null)
        {
            rotateAction.action.Enable();
            rotateAction.action.performed += OnRotatePerformed;
        }

        if (spawnRandomItemAction != null && spawnRandomItemAction.action != null)
        {
            spawnRandomItemAction.action.Enable();
            spawnRandomItemAction.action.performed += OnSpawnRandomItemPerformed;
        }

        if (insertRandomItemAction != null && insertRandomItemAction.action != null)
        {
            insertRandomItemAction.action.Enable();
            insertRandomItemAction.action.performed += OnInsertRandomItemPerformed;
        }

        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            pointerPositionAction.action.Enable();
        }
    }

  // Importante "desenchufar" los eventos (-=) al apagar el script para evitar memory leaks (fugas de memoria).
        
    private void DisableInputActions()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }

        if (rotateAction != null && rotateAction.action != null)
        {
            rotateAction.action.performed -= OnRotatePerformed;
            rotateAction.action.Disable();
        }

        if (spawnRandomItemAction != null && spawnRandomItemAction.action != null)
        {
            spawnRandomItemAction.action.performed -= OnSpawnRandomItemPerformed;
            spawnRandomItemAction.action.Disable();
        }

        if (insertRandomItemAction != null && insertRandomItemAction.action != null)
        {
            insertRandomItemAction.action.performed -= OnInsertRandomItemPerformed;
            insertRandomItemAction.action.Disable();
        }

        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            pointerPositionAction.action.Disable();
        }
    }

    #endregion

    #region Input Callbacks

    // Estas funciones son los receptores de la señal. Cuando pulsas el click, Unity grita 
    // OnInteractPerformed y el código entra por aquí.
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        // Solo procesar si hay un grid seleccionado
        if (selectedItemGrid == null)
        {
            return;
        }

        Vector2Int tileGridPosition = GetTileGridPosition();

        if (selectedItem == null)
        {
            // No tenemos nada en la mano → intentar recoger
            PickUpItem(tileGridPosition);
        }
        else
        {
            // Tenemos algo en la mano → intentar soltar
            PlaceItem(tileGridPosition);
        }
    }

   
    private void OnRotatePerformed(InputAction.CallbackContext context)
    {
        RotateItem();
    }

    
    private void OnSpawnRandomItemPerformed(InputAction.CallbackContext context)
    {
        // Solo spawnear si no tenemos nada en la mano
        if (selectedItem == null)
        {
            CreateRandomItem();
        }
    }

    
    private void OnInsertRandomItemPerformed(InputAction.CallbackContext context)
    {
        InsertRandomItem();
    }

    #endregion

    #region Input Handling 

    // Esta función solo se ejecuta si te olvidas de asignar las acciones en el Inspector de Unity.
    // Sirve como parche para que el juego no se rompa mientras desarrollas.
    private void HandleInputs()
    {
        
        if (AreInputActionsValid())
        {
            return;
        }

        Debug.LogWarning("[InventoryController] InputActionReferences no asignadas. Usando Input clásico como fallback.");

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (selectedItem == null)
            {
                CreateRandomItem();
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            InsertRandomItem();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateItem();
        }

        if (selectedItemGrid != null && Input.GetMouseButtonDown(0))
        {
            Vector2Int tileGridPosition = GetTileGridPosition();

            if (selectedItem == null)
            {
                PickUpItem(tileGridPosition);
            }
            else
            {
                PlaceItem(tileGridPosition);
            }
        }
    }

    #endregion

    #region Item Actions

   
    private void RotateItem()
    {
        if (selectedItem == null)
        {
            return;
        }

        selectedItem.Rotate();

        // Marcar que necesitamos actualizar el highlight
        lastRotationIndex = -1;
    }

   
    private void CreateRandomItem()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[InventoryController] La lista de items está vacía");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("[InventoryController] itemPrefab no asignado");
            return;
        }

        if (canvasTransform == null)
        {
            Debug.LogError("[InventoryController] canvasTransform no asignado");
            return;
        }

        // Instanciar el prefab
        InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();

        if (inventoryItem == null)
        {
            Debug.LogError("[InventoryController] El prefab no tiene componente InventoryItem");
            return;
        }

        selectedItem = inventoryItem;

        // Configurar transform
        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTransform);
        rectTransform.SetAsLastSibling();

        // Asignar datos aleatorios
        int selectedItemID = Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

   
    /// Inserta automáticamente un item aleatorio en el grid seleccionado.
    /// Busca espacio disponible y lo coloca.
   
    private void InsertRandomItem()
    {
        if (selectedItemGrid == null)
        {
            Debug.LogWarning("[InventoryController] No hay grid seleccionado para insertar item");
            return;
        }

        // Crear item aleatorio
        CreateRandomItem();

        if (selectedItem == null)
        {
            return;
        }

        InventoryItem itemToInsert = selectedItem;
        selectedItem = null;

        // Intentar insertar
        InsertItem(itemToInsert);
    }

  
    private void InsertItem(InventoryItem itemToInsert)
    {
        // Le pedimos al grid que escanee toda su matriz buscando un hueco que encaje con la forma
        if (itemToInsert == null || selectedItemGrid == null)
        {
            return;
        }

        // Buscar espacio disponible
        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObject(itemToInsert);

        if (posOnGrid == null)
        {
            Debug.LogWarning("[InventoryController] No hay espacio para el item en el grid");
            Destroy(itemToInsert.gameObject);
            return;
        }

        // Colocar el item
        selectedItemGrid.PlaceItem(itemToInsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

   
    private void PickUpItem(Vector2Int tileGridPosition)
    {
        if (selectedItemGrid == null)
        {
            return;
        }
        // Le robamos el objeto al Grid
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);

        if (selectedItem != null)
        {
            // Lo ponemos por delante en la jerarquía visual para que no quede tapado por otros objetos
            rectTransform = selectedItem.GetComponent<RectTransform>();
            rectTransform.SetAsLastSibling();
        }
    }


    private void PlaceItem(Vector2Int tileGridPosition)
    {
        if (selectedItem == null || selectedItemGrid == null)
        {
            return;
        }

        //// Intentamos colocar el objeto. El Grid nos devuelve "true" si se pudo colocar.
        // Además, si colocamos nuestro objeto Exactamente encima de otro, el Grid nos lo devuelve por referencia (ref overlapItem)
        bool complete = selectedItemGrid.PlaceItem(selectedItem,tileGridPosition.x,tileGridPosition.y,ref overlapItem  );

        if (complete)
        {
            selectedItem = null;

            // Si había un item en esa posición, hacer swap
            if (overlapItem != null)
            {
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
            }
        }
    }

    #endregion

    #region Highlight Logic 

    
    // Actualiza el highlight que muestra dónde caerá el item.
   
    private void HandleHighlight()
    {
        if (inventoryHighlight == null || selectedItemGrid == null)
        {
            return;
        }

        // Obtener posición actual del cursor en el grid
        Vector2Int positionOnGrid = GetTileGridPosition();

        // Obtener rotación actual del item (si hay uno seleccionado)
        int currentRotationIndex = selectedItem != null ? selectedItem.RotationIndex : -1;

        //  Solo actualizar si cambió la posición O la rotación
        bool positionChanged = lastHighlightPosition != positionOnGrid;
        bool rotationChanged = lastRotationIndex != currentRotationIndex;

        if (!positionChanged && !rotationChanged)
        {
            // No ha cambiado nada, no recalcular
            return;
        }

        // Actualizar cache
        lastHighlightPosition = positionOnGrid;
        lastRotationIndex = currentRotationIndex;

        //  No tenemos nada en la mano , pues resaltar el item debajo del cursor
        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);

            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);
            }
            else
            {
                inventoryHighlight.Show(false);
            }
        }
        // Tenemos algo en la mano, pues  resaltar si cabe en esa posición
        else
        {
            bool canPlace = selectedItemGrid.BoundaryCheck( positionOnGrid.x, positionOnGrid.y,selectedItem.WIDTH, selectedItem.HEIGHT );

            inventoryHighlight.Show(canPlace);
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetPosition( selectedItemGrid, selectedItem,positionOnGrid.x,positionOnGrid.y
            );
        }
    }

    #endregion

    #region Position Helpers

 
    // Calcula la posición del cursor en coordenadas de grid.
    // Ajusta el offset según el tamaño del item seleccionado para centrar correctamente.
    
    private Vector2Int GetTileGridPosition()
    {
        Vector2 position = GetPointerPosition();

        // Ajustar offset si tenemos un item seleccionado
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidht / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }

        if (selectedItemGrid == null)
        {
            return Vector2Int.zero;
        }

        Vector2Int tileGridPosition = selectedItemGrid.GetTileGridPosition(position);
        return tileGridPosition;
    }

    
    // Obtiene la posición del puntero (ratón o gamepad).
    // Prioriza el New Input System, con fallback al Input clásico.
    
    private Vector2 GetPointerPosition()
    {
        // Intentar usar New Input System
        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            return pointerPositionAction.action.ReadValue<Vector2>();
        }

        // Fallback al sistema antiguo
        return Input.mousePosition;
    }

   
    // Actualiza la posición del item seleccionado para que siga al cursor.
   
    private void UpdateSelectedItemPosition()
    {
        if (selectedItem != null && rectTransform != null)
        {
            rectTransform.position = GetPointerPosition();
        }
    }

    #endregion

    #region Validation


    // Valida que todas las referencias requeridas estén asignadas.
    // Muestra warnings en la consola si falta algo.
    
    private void ValidateReferences()
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[InventoryController] Lista de items vacía");
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning("[InventoryController] itemPrefab no asignado");
        }

        if (canvasTransform == null)
        {
            Debug.LogWarning("[InventoryController] canvasTransform no asignado");
        }

        if (interactAction == null)
        {
            Debug.LogWarning("[InventoryController] interactAction no asignado");
        }

        if (rotateAction == null)
        {
            Debug.LogWarning("[InventoryController] rotateAction no asignado");
        }

        if (pointerPositionAction == null)
        {
            Debug.LogWarning("[InventoryController] pointerPositionAction no asignado. Se usará Input.mousePosition como fallback.");
        }
    }

  
    // Verifica si todas las InputActionReference críticas están asignadas.
  
    private bool AreInputActionsValid()
    {
        return interactAction != null &&
               rotateAction != null &&
               pointerPositionAction != null;
    }

    #endregion
}