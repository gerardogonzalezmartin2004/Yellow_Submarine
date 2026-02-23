using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Gameplay;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    // Actualizar los elementos visuales del barco(velocidad, oro, inventario)
    public class BoatHUDController : MonoBehaviour
    {
        [Header("Referencias de la escena")]
        [Tooltip("BoatMovement del barco para leer la velocidad actual")]
        [SerializeField] private BoatMovement boatMovement;

        [Header("UI: Velocímetro")]
        [Tooltip("Image en modo Filled para la barra de velocidad")]
        [SerializeField] private Image speedFillImage;

        [Tooltip("Texto que muestra la velocidad en m/s")]
        [SerializeField] private TextMeshProUGUI speedText;

        [Tooltip("Velocidad máxima del barco (para normalizar la barra, debe coincidir con BoatMovement.maxSpeed)")]
        [SerializeField] private float maxSpeed = 8f;

        [Header("UI: Oro")]
        [Tooltip("Texto que muestra el oro actual del jugador")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("UI: Inventario")]
        [Tooltip("Texto que muestra cuántos items hay en la bodega")]
        [SerializeField] private TextMeshProUGUI itemCountText;

        [Tooltip("Texto que muestra el valor total del inventario")]
        [SerializeField] private TextMeshProUGUI inventoryValueText;

        [Header("Colores de velocidad")]
        [Tooltip("Color de la barra cuando el barco va lento")]
        [SerializeField] private Color speedColorLow = new Color(0.29f, 0.60f, 0.55f); 
        [Tooltip("Color de la barra cuando el barco va a máxima velocidad")]
        [SerializeField] private Color speedColorHigh = new Color(0.78f, 0.66f, 0.43f); 

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        #region Unity Lifecycle

        private void Awake()
        {
            
            if (boatMovement == null)
            {
                boatMovement = FindFirstObjectByType<BoatMovement>();

                if (boatMovement == null)
                {
                    Debug.LogWarning("[BoatHUD] BoatMovement no encontrado en la escena.");
                }                   
               
            }
        }
        private void OnEnable()
        {
            // Suscribirse a eventos para actualizar la UI automáticamente
            CurrencyManager.OnGoldChanged += OnGoldChanged;
            InventoryManager.OnInventoryChanged += RefreshInventory;

            // Actualizar valores iniciales al activarse
            RefreshGold();
            RefreshInventory();
        }

        private void OnDisable()
        {
            // Desuscribirse para evitar errores de memoria
           CurrencyManager.OnGoldChanged -= OnGoldChanged;
           InventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }

        

        private void Update()
        {
            UpdateSpeedUI();
        }

        #endregion

        #region UI Updates

        private void UpdateSpeedUI()
        {
            if (boatMovement == null)
            {
                return;
            }

            // Usamos el valor absoluto para que la barra no quede vacía al ir hacia atrás
            float speed = Mathf.Abs(boatMovement.GetCurrentSpeed());
            float normalized = Mathf.Clamp01(speed / maxSpeed);

            if (speedFillImage != null)
            {
                speedFillImage.fillAmount = normalized;
                speedFillImage.color = Color.Lerp(speedColorLow, speedColorHigh, normalized);
            }

            if (speedText != null)
            {
                speedText.text = speed.ToString("F1") + " m/s";
            }
        }

        // Se llama automáticamente cuando cambia el oro
        private void OnGoldChanged(int newAmount, int delta)
        {
            RefreshGold();
        }

        // Se llama automáticamente cuando cambia el inventario
        private void OnInventoryChanged()
        {
            RefreshInventory();
        }

        private void RefreshGold()
        {
            if (goldText == null || CurrencyManager.Instance == null)
            {
                return;
            }

            goldText.text = CurrencyManager.Instance.GetGold() + "G";
        }

        private void RefreshInventory()
        {
            if (InventoryManager.Instance == null)
            {
                return;
            }

            if (itemCountText != null)
            {
                int count = Core.InventoryManager.Instance.GetItemCount();
                itemCountText.text = count + " items";
            }

            if (inventoryValueText != null)
            {
                int value = Core.InventoryManager.Instance.CalculateTotalValue();
                inventoryValueText.text = value + "G";
            }
        }

        #endregion
    }
}