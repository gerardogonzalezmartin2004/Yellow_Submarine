using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using AbyssalReach.Core;
using AbyssalReach.Gameplay;

namespace AbyssalReach.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button sellAllButton;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI inventoryValueText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private Button closeButton;

        [Header("Upgrade Buttons")]
        [SerializeField] private Button upgradeCableLengthButton;
        [SerializeField] private Button upgradeCableStrengthButton;
        [SerializeField] private Button upgradeSwimSpeedButton;

        [Header("Port Reference")]
        [SerializeField] private Gameplay.PortArea portArea;

        [Header("External References")]
        [SerializeField] private TetherSystem tetherSystem;
        [SerializeField] private DiverMovement diverMovement;

        [Header("Upgrade Settings")]
        [SerializeField] private float mejoraLongitudCable;
        [SerializeField] private float mejoraPeso; // Paso A: Campo añadido
        [SerializeField] private float mejoraVelocidad;

        [Header("Boat Grid Reference")]
        [SerializeField] private ItemGrid boatItemGrid;

        #region Unity Lifecycle

        private void OnEnable()
        {
            CurrencyManager.OnGoldChanged += UpdateGoldDisplay;

            if (sellAllButton != null)
                sellAllButton.onClick.AddListener(SellAllItems);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseShop);

            // Paso C: Actualización de Listeners
            if (upgradeCableLengthButton != null)
                upgradeCableLengthButton.onClick.AddListener(PurchaseCableUpgrade);

            if (upgradeCableStrengthButton != null)
                upgradeCableStrengthButton.onClick.AddListener(PurchaseCableStrength);

            if (upgradeSwimSpeedButton != null)
                upgradeSwimSpeedButton.onClick.AddListener(PurchaseSpeedUpgrade);

            UpdateGoldDisplay(0, 0);
            UpdateInventoryDisplay();
        }

        private void OnDisable()
        {
            CurrencyManager.OnGoldChanged -= UpdateGoldDisplay;

            if (sellAllButton != null) sellAllButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (upgradeCableLengthButton != null) upgradeCableLengthButton.onClick.RemoveAllListeners();
            if (upgradeCableStrengthButton != null) upgradeCableStrengthButton.onClick.RemoveAllListeners();
            if (upgradeSwimSpeedButton != null) upgradeSwimSpeedButton.onClick.RemoveAllListeners();
        }

        #endregion

        #region Shop Actions

        private void SellAllItems()
        {
            if (boatItemGrid == null || CurrencyManager.Instance == null)
            {
                Debug.LogError("[ShopUI] Falta boatItemGrid o CurrencyManager");
                return;
            }

            List<InventoryItem> items = boatItemGrid.GetAllItems();
            if (items.Count == 0) return;

            int totalValue = 0;
            foreach (InventoryItem item in items)
                if (item != null && item.itemData != null)
                    totalValue += ItemDamageTracker.Instance != null
                        ? ItemDamageTracker.Instance.GetRuntimeValue(item.itemData)
                        : item.itemData.value;

            if (totalValue <= 0) return;

            CurrencyManager.Instance.AddGold(totalValue);
            if (ItemDamageTracker.Instance != null)
                ItemDamageTracker.Instance.ClearDamages();
            boatItemGrid.ClearAllItems();

            Debug.Log("[ShopUI] Vendido todo por " + totalValue + "G");
            UpdateInventoryDisplay();
        }

        
        private void PurchaseCableUpgrade()
        {
            if (CurrencyManager.Instance == null) return;
            if (CurrencyManager.Instance.SpendGold(50))
            {
                if (tetherSystem != null)
                    tetherSystem.UpgradeCableLength(mejoraLongitudCable);

                Debug.Log("[ShopUI] Cable Length upgraded by " + mejoraLongitudCable);
            }
        }

        private void PurchaseCableStrength()
        {
            if (CurrencyManager.Instance == null) return;
            if (CurrencyManager.Instance.SpendGold(75))
            {
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.GetDiverInventory().UpgradeMaxWeight(mejoraPeso);

                Debug.Log("[ShopUI] Max Weight upgraded by " + mejoraPeso);
            }
        }

        private void PurchaseSpeedUpgrade()
        {
            if (CurrencyManager.Instance == null) return;
            if (CurrencyManager.Instance.SpendGold(100))
            {
                if (diverMovement != null)
                    diverMovement.UpgradeSwimSpeed(mejoraVelocidad);

                Debug.Log("[ShopUI] Swim Speed upgraded by " + mejoraVelocidad);
            }
        }

        private void CloseShop()
        {
            if (portArea != null)
                portArea.CloseShop();
            else
                gameObject.SetActive(false);
        }

        #endregion

        #region UI Updates

        private void UpdateGoldDisplay(int newAmount, int delta)
        {
            if (goldText != null && CurrencyManager.Instance != null)
                goldText.text = "Gold: " + CurrencyManager.Instance.GetGold() + "G";
        }

        private void UpdateInventoryDisplay()
        {
            List<InventoryItem> items = boatItemGrid != null
                ? boatItemGrid.GetAllItems()
                : new List<InventoryItem>();

            int totalValue = 0;
            foreach (InventoryItem item in items)
                if (item != null && item.itemData != null)
                    totalValue += ItemDamageTracker.Instance != null
                        ? ItemDamageTracker.Instance.GetRuntimeValue(item.itemData)
                        : item.itemData.value;

            if (inventoryValueText != null)
                inventoryValueText.text = "Inventory Value: " + totalValue + "G";

            if (itemCountText != null)
                itemCountText.text = "Items: " + items.Count;

            if (sellAllButton != null)
                sellAllButton.interactable = items.Count > 0;
        }

        #endregion
    }
}