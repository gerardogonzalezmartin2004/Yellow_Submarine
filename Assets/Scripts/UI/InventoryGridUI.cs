using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Controlador principal de la UI visual del inventario grid.
    /// Crea y actualiza los slots del grid automáticamente.
    /// Muestra tooltips y estadísticas.
    /// </summary>
    public class InventoryGridUI : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("¿Qué inventario mostrar?")]
        [SerializeField] private InventoryType inventoryType = InventoryType.Boat;

        [Header("Referencias de Prefabs")]
        [Tooltip("Prefab del slot (debe tener ItemSlotUI)")]
        [SerializeField] private GameObject slotPrefab;

        [Header("Referencias de UI")]
        [Tooltip("Contenedor donde se crearán los slots")]
        [SerializeField] private GridLayoutGroup gridContainer;

        [Tooltip("Panel del tooltip")]
        [SerializeField] private GameObject tooltipPanel;

        [Tooltip("Texto del nombre del item en el tooltip")]
        [SerializeField] private TextMeshProUGUI tooltipNameText;

        [Tooltip("Texto de la descripción en el tooltip")]
        [SerializeField] private TextMeshProUGUI tooltipDescriptionText;

        [Tooltip("Texto de las stats (valor, peso)")]
        [SerializeField] private TextMeshProUGUI tooltipStatsText;

        [Header("Textos de Estadísticas")]
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private TextMeshProUGUI totalValueText;
        [SerializeField] private Slider weightSlider;

        [Header("Colores")]
        [SerializeField] private Color normalWeightColor = Color.white;
        [SerializeField] private Color warningWeightColor = Color.yellow;
        [SerializeField] private Color criticalWeightColor = Color.red;

        // Estado interno
        private GridInventory currentInventory;
        private List<ItemSlotUI> slotUIList = new List<ItemSlotUI>();

        public enum InventoryType { Diver, Boat }

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Suscribirse a eventos
            InventoryManager.OnInventoryChanged += RefreshDisplay;

            // Inicializar
            RefreshDisplay();
        }

        private void OnDisable()
        {
            // Desuscribirse
            InventoryManager.OnInventoryChanged -= RefreshDisplay;
        }

        private void Start()
        {
            // Ocultar tooltip al inicio
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }

            // Refresh inicial
            RefreshDisplay();
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Actualiza toda la visualización del inventario
        /// Se llama automáticamente cuando el inventario cambia
        /// </summary>
        public void RefreshDisplay()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("[InventoryGridUI] InventoryManager no encontrado");
                return;
            }

            // Obtener el inventario a mostrar
            currentInventory = (inventoryType == InventoryType.Diver)
                ? InventoryManager.Instance.GetDiverInventory()
                : InventoryManager.Instance.GetBoatInventory();

            if (currentInventory == null)
            {
                Debug.LogWarning("[InventoryGridUI] Inventario nulo");
                return;
            }

            // Recrear el grid
            CreateGridSlots();

            // Actualizar estadísticas
            UpdateStats();
        }

        /// <summary>
        /// Crea los slots visuales del grid
        /// </summary>
        private void CreateGridSlots()
        {
            // Limpiar slots anteriores
            ClearSlots();

            if (slotPrefab == null || gridContainer == null)
            {
                Debug.LogError("[InventoryGridUI] Faltan referencias (slotPrefab o gridContainer)");
                return;
            }

            // Obtener dimensiones del grid
            Vector2Int gridSize = currentInventory.GetGridSize();
            int width = gridSize.x;
            int height = gridSize.y;

            // Configurar el GridLayoutGroup
            gridContainer.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridContainer.constraintCount = width;

            // Crear slots
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Instanciar slot
                    GameObject slotObj = Instantiate(slotPrefab, gridContainer.transform);
                    ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();

                    if (slotUI == null)
                    {
                        Debug.LogError("[InventoryGridUI] El slotPrefab no tiene ItemSlotUI component");
                        Destroy(slotObj);
                        continue;
                    }

                    // Inicializar slot
                    slotUI.Initialize(x, y, this);

                    // Verificar si hay un item en esta posición
                    GridItem item = currentInventory.GetItemAtSlot(x, y);

                    if (item != null)
                    {
                        // Solo mostrar el item en su celda de origen (esquina superior izquierda)
                        if (item.gridPosition.x == x && item.gridPosition.y == y)
                        {
                            slotUI.SetItem(item);
                        }
                        else
                        {
                            // Esta celda está ocupada pero no es el origen del item
                            // La dejamos vacía visualmente para no duplicar el icono
                            slotUI.SetEmpty();
                        }
                    }
                    else
                    {
                        slotUI.SetEmpty();
                    }

                    // Guardar referencia
                    slotUIList.Add(slotUI);
                }
            }
        }

        /// <summary>
        /// Limpia todos los slots anteriores
        /// </summary>
        private void ClearSlots()
        {
            foreach (ItemSlotUI slot in slotUIList)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            slotUIList.Clear();
        }

        /// <summary>
        /// Actualiza los textos de estadísticas
        /// </summary>
        private void UpdateStats()
        {
            if (currentInventory == null) return;

            // Peso
            float currentWeight = currentInventory.GetCurrentWeight();
            float maxWeight = currentInventory.GetMaxWeight();
            float weightPercentage = (maxWeight > 0) ? (currentWeight / maxWeight) : 0f;

            if (weightText != null)
            {
                weightText.text = currentWeight.ToString("F1") + " / " + maxWeight.ToString("F1") + " kg";

                // Cambiar color según el peso
                if (weightPercentage >= 0.9f)
                {
                    weightText.color = criticalWeightColor;
                }
                else if (weightPercentage >= 0.7f)
                {
                    weightText.color = warningWeightColor;
                }
                else
                {
                    weightText.color = normalWeightColor;
                }
            }

            // Slider de peso
            if (weightSlider != null)
            {
                weightSlider.value = weightPercentage;
            }

            // Contador de items
            if (itemCountText != null)
            {
                Vector2Int gridSize = currentInventory.GetGridSize();
                int itemCount = currentInventory.GetItemCount();
                int totalSlots = gridSize.x * gridSize.y;

                itemCountText.text = "Items: " + itemCount + " / " + totalSlots + " slots";
            }

            // Valor total
            if (totalValueText != null)
            {
                int totalValue = currentInventory.CalculateTotalValue();
                totalValueText.text = "Valor Total: " + totalValue + "G";
            }
        }

        #endregion

        #region Tooltip System

        /// <summary>
        /// Muestra el tooltip con la información del item
        /// Llamado por ItemSlotUI cuando haces hover
        /// </summary>
        public void ShowTooltip(LootItemData itemData, Vector3 slotPosition)
        {
            if (tooltipPanel == null || itemData == null) return;

            // Activar panel
            tooltipPanel.SetActive(true);

            // Posicionar tooltip cerca del slot
            // (Ajusta esto según tu layout - puede que necesites RectTransform)
            tooltipPanel.transform.position = slotPosition + new Vector3(150f, 0f, 0f);

            // Actualizar textos
            if (tooltipNameText != null)
            {
                tooltipNameText.text = itemData.itemName;
                tooltipNameText.color = itemData.GetAuraColor();
            }

            if (tooltipDescriptionText != null)
            {
                tooltipDescriptionText.text = itemData.description;
            }

            if (tooltipStatsText != null)
            {
                string stats = "";
                stats += "Valor: " + itemData.value + "G\n";
                stats += "Peso: " + itemData.weight.ToString("F1") + "kg\n";
                stats += "Rareza: " + itemData.rarity.ToString();

                tooltipStatsText.text = stats;
            }
        }

        /// <summary>
        /// Oculta el tooltip
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cambia entre mostrar inventario del diver o del boat
        /// </summary>
        public void SetInventoryType(InventoryType type)
        {
            inventoryType = type;
            RefreshDisplay();
        }

        /// <summary>
        /// Fuerza un refresh manual
        /// </summary>
        public void ForceRefresh()
        {
            RefreshDisplay();
        }

        #endregion
    }
}