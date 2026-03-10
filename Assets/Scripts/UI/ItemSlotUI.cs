using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Representa un SLOT individual del grid de inventario.
    /// Puede estar vacío o contener un item.
    /// Maneja hover, click, y tooltip.
    /// </summary>
    public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Referencias UI")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityBorderImage;

        [Header("Colores")]
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color occupiedSlotColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        // Estado interno
        private GridItem currentItem;
        private int gridX;
        private int gridY;
        private bool isOccupied = false;
        private InventoryGridUI parentGridUI;

        #region Initialization

        /// <summary>
        /// Inicializa el slot con su posición en el grid
        /// Llamado por InventoryGridUI cuando se crea el slot
        /// </summary>
        public void Initialize(int x, int y, InventoryGridUI parent)
        {
            gridX = x;
            gridY = y;
            parentGridUI = parent;

            // Estado inicial: vacío
            SetEmpty();
        }

        #endregion

        #region Update Display

        /// <summary>
        /// Actualiza el slot para mostrar un item
        /// </summary>
        public void SetItem(GridItem item)
        {
            currentItem = item;
            isOccupied = true;

            // Mostrar icono del item
            if (iconImage != null && item.itemData.icon != null)
            {
                iconImage.sprite = item.itemData.icon;
                iconImage.color = Color.white;
                iconImage.enabled = true;
            }

            // Color de fondo según rareza
            if (backgroundImage != null)
            {
                backgroundImage.color = occupiedSlotColor;
            }

            // Borde de rareza
            if (rarityBorderImage != null)
            {
                rarityBorderImage.color = item.itemData.GetAuraColor();
                rarityBorderImage.enabled = true;
            }
        }

        /// <summary>
        /// Actualiza el slot para mostrarlo vacío
        /// </summary>
        public void SetEmpty()
        {
            currentItem = null;
            isOccupied = false;

            // Ocultar icono
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }

            // Color de fondo vacío
            if (backgroundImage != null)
            {
                backgroundImage.color = emptySlotColor;
            }

            // Ocultar borde
            if (rarityBorderImage != null)
            {
                rarityBorderImage.enabled = false;
            }
        }

        #endregion

        #region Mouse Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Resaltar al hacer hover
            if (backgroundImage != null)
            {
                backgroundImage.color = hoverColor;
            }

            // Mostrar tooltip si hay item
            if (isOccupied && currentItem != null && parentGridUI != null)
            {
                parentGridUI.ShowTooltip(currentItem.itemData, transform.position);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Restaurar color original
            if (backgroundImage != null)
            {
                backgroundImage.color = isOccupied ? occupiedSlotColor : emptySlotColor;
            }

            // Ocultar tooltip
            if (parentGridUI != null)
            {
                parentGridUI.HideTooltip();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Click en el slot
            if (isOccupied && currentItem != null)
            {
                Debug.Log("[ItemSlotUI] Click en: " + currentItem.itemData.itemName + " en (" + gridX + ", " + gridY + ")");

                // Aquí puedes añadir lógica de click (ej: drag & drop, vender item individual, etc.)
            }
            else
            {
                Debug.Log("[ItemSlotUI] Click en slot vacío (" + gridX + ", " + gridY + ")");
            }
        }

        #endregion

        #region Getters

        public bool IsOccupied()
        {
            return isOccupied;
        }

        public GridItem GetItem()
        {
            return currentItem;
        }

        public Vector2Int GetGridPosition()
        {
            return new Vector2Int(gridX, gridY);
        }

        #endregion
    }
}