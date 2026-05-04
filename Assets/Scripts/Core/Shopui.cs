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
        [SerializeField] private float mejoraLongitudCable;
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

            if (upgradeCableLengthButton != null)
                upgradeCableLengthButton.onClick.AddListener(PurchaseCableUpgrade);

            if (upgradeCableStrengthButton != null)
                upgradeCableStrengthButton.onClick.AddListener(PurchaseStrengthUpgrade);

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
                    totalValue += item.itemData.value;

            if (totalValue <= 0) return;

            CurrencyManager.Instance.AddGold(totalValue);
            boatItemGrid.ClearAllItems();

            Debug.Log("[ShopUI] Vendido todo por " + totalValue + "G");
            UpdateInventoryDisplay();
        }

        private void PurchaseCableUpgrade() => PurchaseUpgrade("Cable Length", 50);
        private void PurchaseStrengthUpgrade() => PurchaseUpgrade("Cable Strength", 75);
        private void PurchaseSpeedUpgrade() => PurchaseUpgrade("Swim Speed", 100);

        private void PurchaseUpgrade(string upgradeName, int cost)
        {
            if (CurrencyManager.Instance == null) return;

            if (CurrencyManager.Instance.SpendGold(cost))
            {
                Debug.Log("[ShopUI] Purchased: " + upgradeName + " for " + cost + "G");

                if (upgradeName == "Cable Length" && tetherSystem != null)
                    tetherSystem.maxLength += mejoraLongitudCable;
                else if (upgradeName == "Swim Speed" && diverMovement != null)
                    diverMovement.swimSpeed += mejoraVelocidad;
            }
            else
            {
                Debug.Log("[ShopUI] No tienes suficiente oro para " + upgradeName);
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
                    totalValue += item.itemData.value;

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