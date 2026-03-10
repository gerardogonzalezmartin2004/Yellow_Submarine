using System.Collections.Generic;
using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Manager principal del sistema de inventario.
    /// Gestiona dos inventarios grid: Diver (limitado) y Boat (grande).
    /// Singleton para acceso global.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager instance;

        [Header("Grid Inventories")]
        [Tooltip("Inventario del buceador (limitado)")]
        [SerializeField] private GridInventory diverInventory;

        [Tooltip("Inventario del barco (grande)")]
        [SerializeField] private GridInventory boatInventory;

        [Header("Initial Diver Settings")]
        [SerializeField] private int diverGridWidth = 3;
        [SerializeField] private int diverGridHeight = 3;
        [SerializeField] private float diverMaxWeight = 20f;

        [Header("Initial Boat Settings")]
        [SerializeField] private int boatGridWidth = 5;
        [SerializeField] private int boatGridHeight = 5;
        [SerializeField] private float boatMaxWeight = 200f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        #region Events

        // Eventos para mantener compatibilidad con ShopUI y otros sistemas
        public delegate void InventoryChanged();
        public static event InventoryChanged OnInventoryChanged;

        public delegate void ItemAdded(LootItemData item);
        public static event ItemAdded OnItemAdded;

        public delegate void ItemRemoved(LootItemData item);
        public static event ItemRemoved OnItemRemoved;

        #endregion

        #region Singleton

        public static InventoryManager Instance
        {
            get { return instance; }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            // Inicializar inventarios
            InitializeInventories();

            // Cargar inventarios guardados
            LoadInventories();
        }

        private void OnApplicationQuit()
        {
            // Guardar automáticamente al cerrar
            SaveInventories();
        }

        #endregion

        #region Initialization

        private void InitializeInventories()
        {
            // Crear inventario del diver
            diverInventory = new GridInventory(diverGridWidth, diverGridHeight, diverMaxWeight);

            // Crear inventario del boat
            boatInventory = new GridInventory(boatGridWidth, boatGridHeight, boatMaxWeight);

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Inventarios inicializados:");
                Debug.Log("  Diver: " + diverGridWidth + "x" + diverGridHeight + " / " + diverMaxWeight + "kg");
                Debug.Log("  Boat: " + boatGridWidth + "x" + boatGridHeight + " / " + boatMaxWeight + "kg");
            }
        }

        #endregion

        #region Pickup System (Diver)

        /// <summary>
        /// Intenta recoger un item con el buceador
        /// Retorna true si tuvo éxito
        /// </summary>
        public bool TryPickupItem(LootItemData item)
        {
            if (item == null) return false;

            string errorMessage;
            bool success = diverInventory.TryAddItem(item, out errorMessage);

            if (success)
            {
                if (showDebug)
                {
                    Debug.Log("[InventoryManager] Recogido: " + item.itemName + " (Peso: " + item.weight + "kg)");
                }

                // Disparar eventos
                OnItemAdded?.Invoke(item);
                OnInventoryChanged?.Invoke();

                // Guardar
                SaveInventories();
            }
            else
            {
                if (showDebug)
                {
                    Debug.LogWarning("[InventoryManager] No se pudo recoger " + item.itemName + ": " + errorMessage);
                }
            }

            return success;
        }

        /// <summary>
        /// Verifica si el diver puede recoger un item
        /// </summary>
        public bool CanPickupItem(LootItemData item)
        {
            return diverInventory.CanFitItem(item);
        }

        #endregion

        #region Transfer System (Diver → Boat)

        /// <summary>
        /// Transfiere todos los items del diver al boat
        /// Los items que no quepan quedan en el diver (staging area)
        /// Retorna el número de items transferidos exitosamente
        /// </summary>
        public int TransferDiverToBoat()
        {
            List<GridItem> diverItems = diverInventory.GetAllItems();
            int transferredCount = 0;
            List<GridItem> itemsToRemove = new List<GridItem>();

            foreach (GridItem item in diverItems)
            {
                string errorMessage;
                bool success = boatInventory.TryAddItem(item.itemData, out errorMessage);

                if (success)
                {
                    itemsToRemove.Add(item);
                    transferredCount++;
                }
                else
                {
                    if (showDebug)
                    {
                        Debug.LogWarning("[InventoryManager] No se pudo transferir " + item.itemData.itemName + ": " + errorMessage);
                    }
                }
            }

            // Eliminar items transferidos del diver
            foreach (GridItem item in itemsToRemove)
            {
                diverInventory.RemoveItem(item);
            }

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Transferidos " + transferredCount + " de " + diverItems.Count + " items");

                int remaining = diverItems.Count - transferredCount;
                if (remaining > 0)
                {
                    Debug.LogWarning("[InventoryManager] " + remaining + " items quedaron en el diver (staging area)");
                }
            }

            OnInventoryChanged?.Invoke();
            SaveInventories();

            return transferredCount;
        }

        /// <summary>
        /// Descarta items del diver (los tira al mar)
        /// Se usa cuando el jugador confirma desechar items de la staging area
        /// </summary>
        public void DiscardDiverItems()
        {
            int count = diverInventory.GetItemCount();
            diverInventory.Clear();

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Descartados " + count + " items del diver");
            }

            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        #endregion

        #region Selling System (Para ShopUI)

        /// <summary>
        /// Vende todos los items del BOAT y retorna el oro total
        /// IMPORTANTE: ShopUI espera que este método exista
        /// </summary>
        public int SellAllItems()
        {
            int totalValue = boatInventory.CalculateTotalValue();
            int itemCount = boatInventory.GetItemCount();

            boatInventory.Clear();

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Vendidos " + itemCount + " items por " + totalValue + "G");
            }

            OnInventoryChanged?.Invoke();
            SaveInventories();

            return totalValue;
        }

        /// <summary>
        /// Calcula el valor total del BOAT
        /// IMPORTANTE: ShopUI espera que este método exista
        /// </summary>
        public int CalculateTotalValue()
        {
            return boatInventory.CalculateTotalValue();
        }

        /// <summary>
        /// Calcula el peso total del BOAT
        /// </summary>
        public float CalculateTotalWeight()
        {
            return boatInventory.GetCurrentWeight();
        }

        /// <summary>
        /// Retorna el número de items en el BOAT
        /// IMPORTANTE: ShopUI espera que este método exista
        /// </summary>
        public int GetItemCount()
        {
            return boatInventory.GetItemCount();
        }

        /// <summary>
        /// Verifica si el BOAT está vacío
        /// IMPORTANTE: ShopUI espera que este método exista
        /// </summary>
        public bool IsEmpty()
        {
            return boatInventory.IsEmpty();
        }

        /// <summary>
        /// Retorna una lista de items del BOAT (para compatibilidad)
        /// </summary>
        public List<LootItemData> GetItems()
        {
            List<LootItemData> items = new List<LootItemData>();

            foreach (GridItem gridItem in boatInventory.GetAllItems())
            {
                items.Add(gridItem.itemData);
            }

            return items;
        }

        /// <summary>
        /// Limpia el BOAT
        /// </summary>
        public void Clear()
        {
            boatInventory.Clear();
            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        #endregion

        #region Upgrade System

        /// <summary>
        /// Mejora el tamaño del grid del diver
        /// </summary>
        public void UpgradeDiverGrid(int newWidth, int newHeight)
        {
            diverInventory.UpgradeGridSize(newWidth, newHeight);
            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        /// <summary>
        /// Mejora el tamaño del grid del boat
        /// </summary>
        public void UpgradeBoatGrid(int newWidth, int newHeight)
        {
            boatInventory.UpgradeGridSize(newWidth, newHeight);
            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        /// <summary>
        /// Mejora la capacidad de peso del diver
        /// </summary>
        public void UpgradeDiverWeight(float additionalCapacity)
        {
            diverInventory.UpgradeWeightCapacity(additionalCapacity);
            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        /// <summary>
        /// Mejora la capacidad de peso del boat
        /// </summary>
        public void UpgradeBoatWeight(float additionalCapacity)
        {
            boatInventory.UpgradeWeightCapacity(additionalCapacity);
            OnInventoryChanged?.Invoke();
            SaveInventories();
        }

        #endregion

        #region API for UI

        /// <summary>
        /// Obtiene el inventario del diver (para UI)
        /// </summary>
        public GridInventory GetDiverInventory()
        {
            return diverInventory;
        }

        /// <summary>
        /// Obtiene el inventario del boat (para UI)
        /// </summary>
        public GridInventory GetBoatInventory()
        {
            return boatInventory;
        }

        #endregion

        #region Save/Load System

        /// <summary>
        /// Guarda ambos inventarios en PlayerPrefs como JSON
        /// </summary>
        public void SaveInventories()
        {
            try
            {
                string diverJson = JsonUtility.ToJson(diverInventory);
                string boatJson = JsonUtility.ToJson(boatInventory);

                PlayerPrefs.SetString("DiverInventory", diverJson);
                PlayerPrefs.SetString("BoatInventory", boatJson);
                PlayerPrefs.Save();

                if (showDebug)
                {
                    Debug.Log("[InventoryManager] Inventarios guardados");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InventoryManager] Error al guardar: " + e.Message);
            }
        }

        /// <summary>
        /// Carga ambos inventarios desde PlayerPrefs
        /// </summary>
        public void LoadInventories()
        {
            try
            {
                if (PlayerPrefs.HasKey("DiverInventory"))
                {
                    string diverJson = PlayerPrefs.GetString("DiverInventory");
                    GridInventory loadedDiver = JsonUtility.FromJson<GridInventory>(diverJson);

                    if (loadedDiver != null)
                    {
                        diverInventory = loadedDiver;
                    }
                }

                if (PlayerPrefs.HasKey("BoatInventory"))
                {
                    string boatJson = PlayerPrefs.GetString("BoatInventory");
                    GridInventory loadedBoat = JsonUtility.FromJson<GridInventory>(boatJson);

                    if (loadedBoat != null)
                    {
                        boatInventory = loadedBoat;
                    }
                }

                if (showDebug)
                {
                    Debug.Log("[InventoryManager] Inventarios cargados");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[InventoryManager] Error al cargar: " + e.Message);
            }
        }

        /// <summary>
        /// Resetea los inventarios a valores por defecto (útil para debugging)
        /// </summary>
        public void ResetInventories()
        {
            PlayerPrefs.DeleteKey("DiverInventory");
            PlayerPrefs.DeleteKey("BoatInventory");
            InitializeInventories();

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Inventarios reseteados");
            }
        }

        #endregion

        #region Debug Display

        private void OnGUI()
        {
            if (!showDebug) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            int yOffset = 120;

            // === DIVER INVENTORY ===
            GUI.Label(new Rect(10, yOffset, 300, 20), "=== DIVER INVENTORY ===", style);
            yOffset += 25;

            Vector2Int diverSize = diverInventory.GetGridSize();
            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Grid: " + diverSize.x + "x" + diverSize.y + " | Items: " + diverInventory.GetItemCount(), style);
            yOffset += 20;

            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Weight: " + diverInventory.GetCurrentWeight().ToString("F1") + "/" + diverInventory.GetMaxWeight().ToString("F1") + "kg", style);
            yOffset += 20;

            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Value: " + diverInventory.CalculateTotalValue() + "G", style);
            yOffset += 30;

            // === BOAT INVENTORY ===
            GUI.Label(new Rect(10, yOffset, 300, 20), "=== BOAT INVENTORY ===", style);
            yOffset += 25;

            Vector2Int boatSize = boatInventory.GetGridSize();
            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Grid: " + boatSize.x + "x" + boatSize.y + " | Items: " + boatInventory.GetItemCount(), style);
            yOffset += 20;

            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Weight: " + boatInventory.GetCurrentWeight().ToString("F1") + "/" + boatInventory.GetMaxWeight().ToString("F1") + "kg", style);
            yOffset += 20;

            GUI.Label(new Rect(10, yOffset, 300, 20),
                "Value: " + boatInventory.CalculateTotalValue() + "G", style);
        }

        #endregion
    }
}