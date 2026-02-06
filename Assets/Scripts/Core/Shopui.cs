using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Controla la UI de la tienda del puerto.
    /// Permite vender items y comprar upgrades.
    /// 
    /// SETUP:
    /// 1. Crear Canvas en la escena
    /// 2. Crear Panel hijo del Canvas
    /// 3. Añadir este script al Panel
    /// 4. Configurar las referencias UI
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button sellAllButton;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI inventoryValueText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private Button closeButton;

        [Header("Upgrade Buttons (Placeholders)")]
        [SerializeField] private Button upgradeCableLengthButton;
        [SerializeField] private Button upgradeCableStrengthButton;
        [SerializeField] private Button upgradeSwimSpeedButton;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Suscribirse a eventos
            InventoryManager.OnInventoryChanged += UpdateInventoryDisplay;
            CurrencyManager.OnGoldChanged += UpdateGoldDisplay;

            // Configurar botones
            if (sellAllButton != null)
            {
                sellAllButton.onClick.AddListener(SellAllItems);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            // Upgrades (placeholders por ahora)
            if (upgradeCableLengthButton != null)
            {
                upgradeCableLengthButton.onClick.AddListener(() => PurchaseUpgrade("Cable Length", 50));
            }

            if (upgradeCableStrengthButton != null)
            {
                upgradeCableStrengthButton.onClick.AddListener(() => PurchaseUpgrade("Cable Strength", 75));
            }

            if (upgradeSwimSpeedButton != null)
            {
                upgradeSwimSpeedButton.onClick.AddListener(() => PurchaseUpgrade("Swim Speed", 100));
            }

            // Actualizar display inicial
            UpdateAllDisplays();
        }

        private void OnDisable()
        {
            // Desuscribirse
            InventoryManager.OnInventoryChanged -= UpdateInventoryDisplay;
            CurrencyManager.OnGoldChanged -= UpdateGoldDisplay;

            // Limpiar listeners
            if (sellAllButton != null) sellAllButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (upgradeCableLengthButton != null) upgradeCableLengthButton.onClick.RemoveAllListeners();
            if (upgradeCableStrengthButton != null) upgradeCableStrengthButton.onClick.RemoveAllListeners();
            if (upgradeSwimSpeedButton != null) upgradeSwimSpeedButton.onClick.RemoveAllListeners();
        }

        #endregion

        #region Shop Actions

        /// <summary>
        /// Vende todos los items del inventario
        /// </summary>
        private void SellAllItems()
        {
            if (InventoryManager.Instance == null || CurrencyManager.Instance == null)
            {
                Debug.LogError("[ShopUI] Missing InventoryManager or CurrencyManager!");
                return;
            }

            // Obtener valor total
            int totalValue = InventoryManager.Instance.CalculateTotalValue();

            if (totalValue <= 0)
            {
                if (showDebug)
                {
                    Debug.Log("[ShopUI] No items to sell");
                }
                return;
            }

            // Vender todo
            int earnedGold = InventoryManager.Instance.SellAllItems();

            // Añadir oro
            CurrencyManager.Instance.AddGold(earnedGold);

            if (showDebug)
            {
                Debug.Log($"[ShopUI] Sold all items for {earnedGold}G");
            }

            // TODO: Mostrar feedback visual (ej: animación de monedas)
        }

        /// <summary>
        /// Compra un upgrade (placeholder)
        /// </summary>
        private void PurchaseUpgrade(string upgradeName, int cost)
        {
            if (CurrencyManager.Instance == null)
            {
                Debug.LogError("[ShopUI] CurrencyManager not found!");
                return;
            }

            if (CurrencyManager.Instance.SpendGold(cost))
            {
                if (showDebug)
                {
                    Debug.Log($"[ShopUI] Purchased: {upgradeName} for {cost}G");
                }

                // TODO: Aplicar el upgrade real
                // Ejemplo: TetherSystem.Instance.UpgradeLength(40f);

                // TODO: Mostrar feedback visual
            }
            else
            {
                if (showDebug)
                {
                    Debug.LogWarning($"[ShopUI] Not enough gold for {upgradeName}");
                }

                // TODO: Mostrar mensaje de error
            }
        }

        /// <summary>
        /// Cierra la tienda
        /// </summary>
        private void CloseShop()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region UI Updates

        private void UpdateAllDisplays()
        {
            UpdateGoldDisplay(0, 0); // Los valores reales se obtienen de los managers
            UpdateInventoryDisplay();
        }

        private void UpdateGoldDisplay(int newAmount, int delta)
        {
            if (goldText != null && CurrencyManager.Instance != null)
            {
                goldText.text = $"Gold: {CurrencyManager.Instance.GetGold()}G";
            }
        }

        private void UpdateInventoryDisplay()
        {
            if (InventoryManager.Instance == null) return;

            // Actualizar valor total
            if (inventoryValueText != null)
            {
                int totalValue = InventoryManager.Instance.CalculateTotalValue();
                inventoryValueText.text = $"Inventory Value: {totalValue}G";
            }

            // Actualizar cantidad de items
            if (itemCountText != null)
            {
                int itemCount = InventoryManager.Instance.ItemCount;
                itemCountText.text = $"Items: {itemCount}";
            }

            // Habilitar/deshabilitar botón de venta
            if (sellAllButton != null)
            {
                sellAllButton.interactable = !InventoryManager.Instance.IsEmpty;
            }
        }

        #endregion
    }
}