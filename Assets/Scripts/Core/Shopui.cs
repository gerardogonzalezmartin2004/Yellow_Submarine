using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;
using AbyssalReach.Gameplay;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Controla la UI de la tienda del puerto.
    /// Permite vender items y comprar upgrades.
    /// ACTUALIZADO: Maneja el cierre completo sin script adicional.
    /// </summary>
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
        [Tooltip("Referencia al PortArea para notificar cierre")]
        [SerializeField] private PortArea portArea;

        [Header("External References")]
        [SerializeField] private TetherSystem tetherSystem;
        [SerializeField] private DiverMovement diverMovement;
        [SerializeField] private float mejoraLongitudCable = 5f;
        [SerializeField] private float mejoraVelocidad = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        #region Unity Lifecycle

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

            LogDebug("Tienda abierta - UI inicializada");
        }

        private void OnDisable()
        {
            // Desuscribirse de eventos y así evitamos errores de memoria
            InventoryManager.OnInventoryChanged -= UpdateInventoryDisplay;
            CurrencyManager.OnGoldChanged -= UpdateGoldDisplay;

            // Limpiar listeners de los botones
            if (sellAllButton != null) sellAllButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
            if (upgradeCableLengthButton != null) upgradeCableLengthButton.onClick.RemoveAllListeners();
            if (upgradeCableStrengthButton != null) upgradeCableStrengthButton.onClick.RemoveAllListeners();
            if (upgradeSwimSpeedButton != null) upgradeSwimSpeedButton.onClick.RemoveAllListeners();

            LogDebug("Tienda cerrada - UI limpiada");
        }

        private void Update()
        {
            // Permitir cerrar con ESC también
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseShop();
            }
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

            // Calcular cuánto valen todos los objetos
            int totalValue = InventoryManager.Instance.CalculateTotalValue();

            if (totalValue <= 0)
            {
                LogDebug("No hay items para vender");
                return; // No hay nada que vender
            }

            // Vender items 
            int earnedGold = InventoryManager.Instance.SellAllItems();

            // Añadir el oro ganado al jugador
            CurrencyManager.Instance.AddGold(earnedGold);

            LogDebug($"Vendidos todos los items por {earnedGold}G");
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
                Debug.LogError("[ShopUI] CurrencyManager no encontrado");
                return;
            }

            // Intentar gastar el oro. Si devuelve true, la compra fue exitosa.
            if (CurrencyManager.Instance.SpendGold(cost))
            {
                LogDebug($"Comprado: {upgradeName} por {cost}G");

                // Aplicar la mejora
                if (upgradeName == "Cable Length")
                {
                    if (tetherSystem != null)
                    {
                        tetherSystem.maxLength += mejoraLongitudCable;
                        LogDebug($"Longitud de cable mejorada: {tetherSystem.maxLength}");
                    }
                }
                else if (upgradeName == "Cable Strength")
                {
                    // TODO: Implementar mejora de resistencia
                    LogDebug("Mejora de resistencia comprada (TODO: implementar)");
                }
                else if (upgradeName == "Swim Speed")
                {
                    if (diverMovement != null)
                    {
                        diverMovement.swimSpeed += mejoraVelocidad;
                        LogDebug($"Velocidad de nado mejorada: {diverMovement.swimSpeed}");
                    }
                }
            }
            else
            {
                LogDebug($"Oro insuficiente para {upgradeName} (cuesta {cost}G)");
            }
        }

        /// <summary>
        /// NUEVO: Cierra la tienda y notifica al PortArea.
        /// Maneja TODA la lógica de cierre sin necesidad de script adicional.
        /// </summary>
        private void CloseShop()
        {
            LogDebug("Cerrando tienda...");

            // Notificar al PortArea para que maneje el cooldown y estados
            if (portArea != null)
            {
                portArea.CloseShop();
            }
            else
            {
                // FALLBACK: Si no hay referencia al PortArea, cerrar manualmente
                Debug.LogWarning("[ShopUI] No hay referencia a PortArea - cerrando manualmente");

                // Desactivar el panel
                gameObject.SetActive(false);

                // Reactivar controles del barco
                if (GameController.Instance != null)
                {
                    GameController.Instance.SetGameState(GameController.GameState.Sailing);
                }
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

            // Mostrar valor total del inventario
            if (inventoryValueText != null)
            {
                int totalValue = InventoryManager.Instance.CalculateTotalValue();
                inventoryValueText.text = "Inventory Value: " + totalValue + "G";
            }

            // Mostrar número de items
            if (itemCountText != null)
            {
                int itemCount = InventoryManager.Instance.GetItemCount();
                itemCountText.text = "Items: " + itemCount;
            }

            // Activar/desactivar botón de vender
            if (sellAllButton != null)
            {
                bool hasItems = !InventoryManager.Instance.IsEmpty();
                sellAllButton.interactable = hasItems;
            }
        }

        #endregion

        #region Utilities

        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[ShopUI] {message}");
            }
        }

        #endregion

        #region Public API (Para PortArea)

        /// <summary>
        /// Método público para que PortArea pueda cerrar la tienda externamente.
        /// </summary>
        public void ForceClose()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}