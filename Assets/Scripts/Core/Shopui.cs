using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;
using AbyssalReach.Gameplay;

namespace AbyssalReach.UI
{
    // ShopUI  - Cierre funcional con ratón, gamepad y Escape
   
    public class ShopUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Botón para vender todos los items")]
        [SerializeField] private Button sellAllButton;

        [Tooltip("Botón para cerrar la tienda")]
        [SerializeField] private Button closeButton;

        [Tooltip("Botón para mejorar longitud del cable")]
        [SerializeField] private Button upgradeCableLengthButton;

        [Tooltip("Botón para mejorar resistencia del cable")]
        [SerializeField] private Button upgradeCableStrengthButton;

        [Tooltip("Botón para mejorar velocidad de nado")]
        [SerializeField] private Button upgradeSwimSpeedButton;

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI inventoryValueText;
        [SerializeField] private TextMeshProUGUI itemCountText;

        [Header("Navigation")]
        [Tooltip("Botón que se selecciona al abrir (para gamepad)")]
        [SerializeField] private Button firstSelectedButton;

        [Header("Port Reference")]
        [Tooltip("Referencia al puerto para cerrar correctamente")]
        [SerializeField] private PortArea portArea;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

       

        // Referencia al UI Navigation Manager
        private UINavigationManager navManager;

        // Controles para escuchar Cancel
        private AbyssalReachControls controls;

        #region Unity Lifecycle

        private void Awake()
        {
            // Inicializar controles
            controls = new AbyssalReachControls();

            // Buscar el UINavigationManager
            navManager = UINavigationManager.Instance;

            if (navManager == null)
            {
                Debug.LogError("[ShopUI] UINavigationManager no encontrado");
            }

            // Validar firstSelectedButton
            if (firstSelectedButton == null)
            {
                if (sellAllButton != null)
                {
                    firstSelectedButton = sellAllButton;
                }
                else if (closeButton != null)
                {
                    firstSelectedButton = closeButton;
                }

                if (showDebug && firstSelectedButton != null)
                {
                    Debug.Log("[ShopUI] firstSelectedButton auto-asignado a: " + firstSelectedButton.name);
                }
            }

            // FIX: Asignar listeners a los botones EN AWAKE
            SetupButtonListeners();
        }

        private void OnEnable()
        {
            
            // Habilitar controles y suscribirse a Cancel
            controls.Enable();
            controls.UI.Enable();
            controls.UI.Cancel.performed += OnCancelPressed;

            

            // Suscribirse a eventos de managers
            if (InventoryManager.Instance != null)
            {
                InventoryManager.OnInventoryChanged += UpdateInventoryDisplay;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.OnGoldChanged += UpdateGoldDisplay;
            }

            // Actualizar displays
            UpdateAllDisplays();

            // Abrir panel con navegación
            if (navManager != null)
            {
                navManager.OpenPanel(gameObject, firstSelectedButton.gameObject);
            }
            else
            {
                Debug.LogWarning("[ShopUI] Navegación no configurada");
            }
        }

        private void OnDisable()
        {
            
            // Desuscribirse de Cancel
            controls.UI.Cancel.performed -= OnCancelPressed;
            controls.UI.Disable();
            controls.Disable();

            if (showDebug)
            {
                Debug.Log("[ShopUI] OnDisable - Controles UI deshabilitados");
            }

            // Desuscribirse de managers
            if (InventoryManager.Instance != null)
            {
                InventoryManager.OnInventoryChanged -= UpdateInventoryDisplay;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.OnGoldChanged -= UpdateGoldDisplay;
            }

            // Decirle al Manager que cierre este panel
            if (navManager != null)
            {
                navManager.ClosePanel(gameObject);
            }
        }

        #endregion

        #region Button Setup

        //  Configurar listeners de botones programáticamente
        private void SetupButtonListeners()
        {
            if (sellAllButton != null)
            {
                // Limpiar listeners anteriores
                sellAllButton.onClick.RemoveAllListeners();
                // Añadir nuevo listener
                sellAllButton.onClick.AddListener(SellAllItems);

                if (showDebug)
                {
                    Debug.Log("[ShopUI] Listener asignado a sellAllButton");
                }
            }
            else
            {
                Debug.LogWarning("[ShopUI] sellAllButton no asignado");
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);

                if (showDebug)
                {
                    Debug.Log("[ShopUI] Listener asignado a closeButton");
                }
            }
            else
            {
                Debug.LogWarning("[ShopUI] closeButton no asignado");
            }

            if (upgradeCableLengthButton != null)
            {
                upgradeCableLengthButton.onClick.RemoveAllListeners();
                upgradeCableLengthButton.onClick.AddListener(PurchaseCableUpgrade);
            }

            if (upgradeCableStrengthButton != null)
            {
                upgradeCableStrengthButton.onClick.RemoveAllListeners();
                upgradeCableStrengthButton.onClick.AddListener(PurchaseStrengthUpgrade);
            }

            if (upgradeSwimSpeedButton != null)
            {
                upgradeSwimSpeedButton.onClick.RemoveAllListeners();
                upgradeSwimSpeedButton.onClick.AddListener(PurchaseSpeedUpgrade);
            }
        }

        #endregion

        #region Input Callbacks

        // FIX: Nuevo callback para cerrar con Escape/Cancel
        private void OnCancelPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] Cancel presionado (Escape) - Cerrando tienda");
            }

            CloseShop();
        }

        #endregion

        #region Shop Actions

        private void SellAllItems()
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] SellAllItems llamado");
            }

            if (InventoryManager.Instance == null || CurrencyManager.Instance == null)
            {
                Debug.LogError("[ShopUI] Falta un manager (Inventory o Currency)");
                return;
            }

            int totalValue = InventoryManager.Instance.CalculateTotalValue();

            if (totalValue <= 0)
            {
                if (showDebug)
                {
                    Debug.Log("[ShopUI] No hay items para vender");
                }
                return;
            }

            int earnedGold = InventoryManager.Instance.SellAllItems();
            CurrencyManager.Instance.AddGold(earnedGold);

            if (showDebug)
            {
                Debug.Log("[ShopUI] Vendido todo por " + earnedGold + "G");
            }
        }

        private void PurchaseCableUpgrade()
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] PurchaseCableUpgrade llamado");
            }

            PurchaseUpgrade("Cable Length", 50);
        }

        private void PurchaseStrengthUpgrade()
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] PurchaseStrengthUpgrade llamado");
            }

            PurchaseUpgrade("Cable Strength", 75);
        }

        private void PurchaseSpeedUpgrade()
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] PurchaseSpeedUpgrade llamado");
            }

            PurchaseUpgrade("Swim Speed", 100);
        }

        private void PurchaseUpgrade(string upgradeName, int cost)
        {
            if (CurrencyManager.Instance == null)
            {
                return;
            }

            if (CurrencyManager.Instance.SpendGold(cost))
            {
                Debug.Log("[ShopUI] Comprado: " + upgradeName + " por " + cost + "G");
                // TODO: Aplicar mejora según tu sistema de upgrades
            }
            else
            {
                if (showDebug)
                {
                    Debug.Log("[ShopUI] No tienes suficiente oro para " + upgradeName);
                }
            }
        }

        private void CloseShop()
        {
            if (showDebug)
            {
                Debug.Log("[ShopUI] CloseShop llamado");
            }

            if (portArea != null)
            {
                portArea.CloseShop();
            }
            else
            {
                // Fallback si no hay referencia al puerto
                Debug.LogWarning("[ShopUI] portArea no asignado - cerrando panel directamente");
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region UI Updates

        private void UpdateAllDisplays()
        {
            UpdateGoldDisplay(0, 0);
            UpdateInventoryDisplay();
        }

        private void UpdateGoldDisplay(int newAmount, int delta)
        {
            if (goldText != null && CurrencyManager.Instance != null)
            {
                int currentGold = CurrencyManager.Instance.GetGold();
                goldText.text = "Gold: " + currentGold + "G";
            }
        }

        private void UpdateInventoryDisplay()
        {
            if (InventoryManager.Instance == null)
            {
                return;
            }

            if (inventoryValueText != null)
            {
                int totalValue = InventoryManager.Instance.CalculateTotalValue();
                inventoryValueText.text = "Inventory Value: " + totalValue + "G";
            }

            if (itemCountText != null)
            {
                int itemCount = InventoryManager.Instance.GetItemCount();
                itemCountText.text = "Items: " + itemCount;
            }

            // Actualizar estado del botón de vender
            if (sellAllButton != null)
            {
                bool hasItems = !InventoryManager.Instance.IsEmpty();
                sellAllButton.interactable = hasItems;
            }
        }

        #endregion

        #region Debug

        private void Update()
        {
            if (!showDebug)
            {
                return;
            }

            // Debug adicional para detectar inputs
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    Debug.Log("[ShopUI] Escape detectado por Keyboard.current");
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            style.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 590, 400, 20), "[ShopUI] UI Controls enabled: " + controls.UI.enabled, style);
            GUI.Label(new Rect(10, 610, 400, 20), "[ShopUI] Panel activo: " + gameObject.activeSelf, style);

            if (closeButton != null)
            {
                GUI.Label(new Rect(10, 630, 400, 20), "[ShopUI] CloseButton interactable: " + closeButton.interactable, style);
            }
        }

        #endregion
    }
}