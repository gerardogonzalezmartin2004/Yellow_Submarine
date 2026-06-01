using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
        [SerializeField] private Button upgradeGridButton;

        [Header("Port Reference")]
        [SerializeField] private Gameplay.PortArea portArea;

        [Header("External References")]
        [SerializeField] private TetherSystem tetherSystem;
        [SerializeField] private DiverMovement diverMovement;

        [Header("Upgrade Settings")]
        [SerializeField] private float mejoraLongitudCable;
        [SerializeField] private float mejoraPeso;
        [SerializeField] private float mejoraVelocidad;
        [SerializeField] private int   gridUpgradeCost    = 150;
        [SerializeField] private int   gridUpgradeColumns = 2;
        [SerializeField] private int   maxGridWidth       = 20;

        [Header("Boat Grid Reference")]
        [SerializeField] private ItemGrid boatItemGrid;

        [Header("Cuota (Final del Juego)")]
        [SerializeField] private Button cuotaButton;
        [Tooltip("Coste en oro necesario para pagar la cuota y completar el juego.")]
        [SerializeField] private int cuotaCost = 1000;
        [Tooltip("Panel opcional que se muestra antes de volver al menu. Dejalo vacio para ir directo.")]
        [SerializeField] private GameObject victoryPanel;

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

            if (upgradeGridButton != null)
                upgradeGridButton.onClick.AddListener(PurchaseGridUpgrade);

            if (cuotaButton != null)
                cuotaButton.onClick.AddListener(PurchaseCuota);

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
            if (upgradeGridButton != null) upgradeGridButton.onClick.RemoveAllListeners();
            if (cuotaButton       != null) cuotaButton.onClick.RemoveAllListeners();
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
            AudioManager.Instance.PlaySFX("Vender");
        }

        
        private void PurchaseCableUpgrade()
        {
            if (CurrencyManager.Instance == null) return;
            if (CurrencyManager.Instance.SpendGold(50))
            {
                if (tetherSystem != null)
                    tetherSystem.UpgradeCableLength(mejoraLongitudCable);
                AudioManager.Instance.PlaySFX("Vender");

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
                AudioManager.Instance.PlaySFX("Vender");

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
                AudioManager.Instance.PlaySFX("Vender");

                Debug.Log("[ShopUI] Swim Speed upgraded by " + mejoraVelocidad);
            }
        }

        private void PurchaseGridUpgrade()
        {
            if (boatItemGrid == null || CurrencyManager.Instance == null) return;

            if (boatItemGrid.GetWidth() >= maxGridWidth)
            {
                Debug.Log("[ShopUI] Grid ya al maximo de " + maxGridWidth + " columnas.");
                return;
            }

            if (CurrencyManager.Instance.SpendGold(gridUpgradeCost))
            {
                boatItemGrid.ExpandGrid(gridUpgradeColumns);
                RefreshUpgradeGridButton();
                AudioManager.Instance.PlaySFX("Vender");
                Debug.Log("[ShopUI] Grid ampliado a " + boatItemGrid.GetWidth() + "x" + boatItemGrid.GetHeight());
            }
        }

        private void RefreshUpgradeGridButton()
        {
            if (upgradeGridButton == null || boatItemGrid == null) return;
            upgradeGridButton.interactable = boatItemGrid.GetWidth() < maxGridWidth;
        }

        private void PurchaseCuota()
        {
            if (CurrencyManager.Instance == null) return;

            if (!CurrencyManager.Instance.SpendGold(cuotaCost))
            {
                Debug.Log("[ShopUI] Oro insuficiente para pagar la cuota. Necesitas " + cuotaCost + "G.");
                return;
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("Vender");

            Debug.Log("[ShopUI] Cuota pagada. Juego completado.");

            if (victoryPanel != null)
                StartCoroutine(VictorySequence());
            else
                ReturnToMainMenu();
        }

        private IEnumerator VictorySequence()
        {
            victoryPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(3f);
            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.GoToMainMenu();
            else
            {
                Debug.LogWarning("[ShopUI] SceneLoader no encontrado. Cargando MainMenu directamente.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
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

            if (cuotaButton != null && CurrencyManager.Instance != null)
                cuotaButton.interactable = CurrencyManager.Instance.HasEnoughGold(cuotaCost);
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

            RefreshUpgradeGridButton();
        }

        #endregion
    }
}