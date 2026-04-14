using System;
using UnityEngine;
using UnityEngine.UI;


// Representa un item individual en el inventario.
// Maneja la rotación en 4 direcciones (0°, 90°, 180°, 270°) y el ajuste dinámico de tamaño.

public class InventoryItem : MonoBehaviour
{
    #region Serialized Fields

    [Header("Item Data")]
    [Tooltip("ScriptableObject que contiene los datos del item (tamaño, icono, etc.)")]
    public ItemData itemData;

    #endregion

    #region Private Fields


    // Índice de rotación actual (0 = 0°, 1 = 90°, 2 = 180°, 3 = 270°).
    // Se usa módulo 4 para mantenerlo siempre en el rango válido.

    private int rotationIndex = 0;


    // Cache del RectTransform para evitar GetComponent en cada frame.

    private RectTransform cachedRectTransform;


    // Cache del Image component para setear el sprite solo una vez.

    private Image cachedImage;

    #endregion

    #region Public Properties


    // Altura efectiva del item considerando la rotación actual.
    // Si el índice de rotación es impar (1 o 3), se invierten width y height.

    public int HEIGHT
    {
        get
        {
            if (itemData == null)
            {
                return 1;
            }

            // Si rotationIndex es par (0, 2) - altura original
            // Si rotationIndex es impar (1, 3) - ancho original (porque está girado)
            return IsRotationIndexOdd() ? itemData.width : itemData.height;
        }
    }


    // Anchura efectiva del item considerando la rotación actual.
    // Si el índice de rotación es impar (1 o 3), se invierten width y height.

    public int WIDTH
    {
        get
        {
            if (itemData == null)
            {
                Debug.LogError("[InventoryItem] itemData es null. Asegúrate de llamar Set() antes de acceder a WIDTH.");
                return 1;
            }

            // Si rotationIndex es par (0, 2) - ancho original
            // Si rotationIndex es impar (1, 3) - altura original (porque está girado)
            return IsRotationIndexOdd() ? itemData.height : itemData.width;
        }
    }


    // Posición X en el grid donde está colocado este item.

    public int onGridPositionX;


    // Posición Y en el grid donde está colocado este item.

    public int onGridPositionY;


    // Índice de rotación actual (0-3) para acceso externo.
    // Se expone para que otros sistemas puedan comprobar si ha cambiado.

    public int RotationIndex => rotationIndex;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Cachear componentes en Awake para mejor rendimiento
        cachedRectTransform = GetComponent<RectTransform>();
        cachedImage = GetComponent<Image>();

        // Validación de componentes requeridos
        if (cachedRectTransform == null)
        {
            Debug.LogError("[InventoryItem] Falta RectTransform en " + gameObject.name);
        }
        if (cachedImage == null)
        {
            Debug.LogError("[InventoryItem] Falta Image component en " + gameObject.name);
        }
    }

    #endregion

    #region Public Methods


    // Rota el item 90° en sentido horario.
    // Incrementa el índice de rotación y actualiza la rotación visual del RectTransform.

    public void Rotate()
    {
        // Incrementar rotación (0 → 1 → 2 → 3 → 0)
        rotationIndex = (rotationIndex + 1) % 4;

        // Actualizar rotación visual
        UpdateVisualRotation();
    }


    // Inicializa el item con los datos proporcionados.
    // Configura el sprite y ajusta el tamaño del RectTransform según las dimensiones del item.

    public void Set(ItemData data)
    {
        // Validación
        if (data == null)
        {
            Debug.LogError("[InventoryItem] Se intentó hacer Set con ItemData null");
            return;
        }

        // Asignar data
        itemData = data;

        // Configurar sprite
        if (cachedImage != null && itemData.itemIcon != null)
        {
            cachedImage.sprite = itemData.itemIcon;
        }
        else if (itemData.itemIcon == null)
        {
            Debug.LogWarning("[InventoryItem] itemIcon es null en " + itemData.name);
        }

        // Ajustar tamaño según las dimensiones actuales considerando rotación
        UpdateSize();

        // Asegurar que la rotación visual esté sincronizada
        UpdateVisualRotation();
    }

    #endregion

    #region Private Methods


    // Actualiza la rotación visual del RectTransform.
    // Cada índice representa 90° adicionales (0° → 90° → 180° → 270°).

    private void UpdateVisualRotation()
    {
        if (cachedRectTransform == null)
        {
            return;
        }

        // Calcular ángulo: cada índice = -90° (sentido horario)
        float angle = -90f * rotationIndex;

        // Aplicar rotación
        cachedRectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }


    // Actualiza el tamaño del RectTransform según las dimensiones actuales del item.
    // Tiene en cuenta la rotación para calcular WIDTH y HEIGHT correctamente.

    private void UpdateSize()
    {
        if (cachedRectTransform == null || itemData == null)
        {
            return;
        }

        // Calcular tamaño en píxeles basándose en las propiedades WIDTH y HEIGHT
        Vector2 size = new Vector2
        {
            x = WIDTH * ItemGrid.tileSizeWidht,
            y = HEIGHT * ItemGrid.tileSizeHeight
        };

        cachedRectTransform.sizeDelta = size;
    }


    // Comprueba si el índice de rotación actual es impar (1 o 3).
    // Se usa para determinar si width y height están intercambiados.

    private bool IsRotationIndexOdd()
    {
        return rotationIndex % 2 != 0;
    }

    #endregion

    #region Debug Helpers


    // Dibuja un gizmo en el editor para visualizar el área del item.
    // Útil para debugging.

    private void OnDrawGizmosSelected()
    {
        if (itemData == null) return;

        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(WIDTH * ItemGrid.tileSizeWidht, HEIGHT * ItemGrid.tileSizeHeight, 1f);
        Gizmos.DrawWireCube(center, size);
    }


    #endregion
}