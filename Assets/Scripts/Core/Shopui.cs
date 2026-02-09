using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    public class ShopUI : MonoBehaviour
    {
        // Controla la UI de la tienda del puerto
        // Permite vender items y comprar upgrades

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
        [Tooltip("Referencia al PortArea para notificar cierre")]
        [SerializeField] private Gameplay.PortArea portArea;

        #region Unity ciclo de vida

        private void OnEnable()
        {
            // Suscribirse a eventos para actualizar la UI automáticamente
            InventoryManager.OnInventoryChanged += UpdateInventoryDisplay;
            CurrencyManager.OnGoldChanged += UpdateGoldDisplay;

            // Configurar botones principales
            if (sellAllButton != null)
            {
                sellAllButton.onClick.AddListener(SellAllItems);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            // Configurar botones de mejora 
            if (upgradeCableLengthButton != null)
            {
                upgradeCableLengthButton.onClick.AddListener(PurchaseCableUpgrade);
            }

            if (upgradeCableStrengthButton != null)
            {
                upgradeCableStrengthButton.onClick.AddListener(PurchaseStrengthUpgrade);
            }

            if (upgradeSwimSpeedButton != null)
            {
                upgradeSwimSpeedButton.onClick.AddListener(PurchaseSpeedUpgrade);
            }

            // Actualizar toda la información al abrir la tienda
            UpdateAllDisplays();
        }

        private void OnDisable()
        {
            // Desuscribirse de eventos y asi evitamos errores de memoria
            InventoryManager.OnInventoryChanged -= UpdateInventoryDisplay;
            CurrencyManager.OnGoldChanged -= UpdateGoldDisplay;

            // Limpiar listeners de los botones
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
            // Verificación de seguridad
            if (InventoryManager.Instance == null || CurrencyManager.Instance == null)
            {
                Debug.LogError("[ShopUI] Falta el manager");
                return;
            }

            //  Calcular cuánto valen todos los objetos
            int totalValue = InventoryManager.Instance.CalculateTotalValue();

            if (totalValue <= 0)
            {
                return; // No hay nada que vender
            }

            // Vender items 
            int earnedGold = InventoryManager.Instance.SellAllItems();

            // Añadir el oro ganado al jugador
            CurrencyManager.Instance.AddGold(earnedGold);
        }

        // Los botones de compra

        private void PurchaseCableUpgrade()
        {
            PurchaseUpgrade("Cable Length", 50);
        }

        private void PurchaseStrengthUpgrade()
        {
            PurchaseUpgrade("Cable Strength", 75);
        }

        private void PurchaseSpeedUpgrade()
        {
            PurchaseUpgrade("Swim Speed", 100);
        }

        // Lógica genérica de compra
        private void PurchaseUpgrade(string upgradeName, int cost)
        {
            if (CurrencyManager.Instance == null)
            {
                return;
            }

            // Intentar gastar el oro. Si devuelve true, la compra fue exitosa.
            if (CurrencyManager.Instance.SpendGold(cost))
            {
                Debug.Log("[ShopUI] Purchased: " + upgradeName + " for " + cost + "G");
                // Aquí tendriamos q introducir la lógica real de aplicar la mejora
            }
            else
            {
                Debug.Log("[ShopUI] No tienes suficiente oro para " + upgradeName);
            }
        }

        private void CloseShop()
        {
            // Notificar al PortArea para que maneje el cierre y el cooldown
            if (portArea != null)
            {
                portArea.CloseShop();
            }
            else
            {
                // Si no hay referencia al PortArea, simplemente cerramos la UI
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region UI Updates

        private void UpdateAllDisplays()
        {
            // Actualizamos todo a la vez
            UpdateGoldDisplay(0, 0); // Pasamos 0,0 porque solo queremos repintar el valor actual
            UpdateInventoryDisplay();
        }

        // Se llama automáticamente cuando cambia el oro con el evento
        private void UpdateGoldDisplay(int newAmount, int delta)
        {
            if (goldText != null && CurrencyManager.Instance != null)
            {
                // Damos formato al texto para mostrar el oro actual
                goldText.text = "Gold: " + CurrencyManager.Instance.GetGold() + "G";
            }
        }

        // Se llama automáticamente cuando cambia el inventario con el evento
        private void UpdateInventoryDisplay()
        {
            if (InventoryManager.Instance == null)
            {
                return;
            }

            //  Mostrar valor total del inventario
            if (inventoryValueText != null)
            {
                int totalValue = InventoryManager.Instance.CalculateTotalValue();
                inventoryValueText.text = "Inventory Value: " + totalValue + "G";
            }

           
            //  Mostrar número de items
            if (itemCountText != null)
            {
                int itemCount = InventoryManager.Instance.GetItemCount();
                itemCountText.text = "Items: " + itemCount;
            }

           
            //  Activar/desactivar botón de vender
            if (sellAllButton != null)
            {
                bool hasItems = !InventoryManager.Instance.IsEmpty();
                sellAllButton.interactable = hasItems;
            }
        }

        #endregion
    }
}