using System.Collections.Generic;
using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Singleton que gestiona el inventario de items recolectados.
    /// Almacena temporalmente los items hasta que se vendan en el puerto.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Current Inventory")]
        [SerializeField] private List<LootItemData> collectedItems = new List<LootItemData>();

        [Header("Capacity")]
        [Tooltip("Máximo número de items que puede llevar (0 = ilimitado)")]
        [SerializeField] private int maxCapacity = 0;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        #region Events

        // Eventos para notificar cambios (para UI)
        public delegate void InventoryChanged();
        public static event InventoryChanged OnInventoryChanged;

        public delegate void ItemAdded(LootItemData item);
        public static event ItemAdded OnItemAdded;

        public delegate void ItemRemoved(LootItemData item);
        public static event ItemRemoved OnItemRemoved;

        #endregion

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Añade un item al inventario
        /// </summary>
        /// <returns>True si se añadió exitosamente, False si está lleno</returns>
        public bool AddItem(LootItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[InventoryManager] Attempted to add null item");
                return false;
            }

            // Verificar capacidad
            if (maxCapacity > 0 && collectedItems.Count >= maxCapacity)
            {
                if (showDebug)
                {
                    Debug.LogWarning($"[InventoryManager] Inventory full! ({collectedItems.Count}/{maxCapacity})");
                }
                return false;
            }

            // Añadir item
            collectedItems.Add(item);

            if (showDebug)
            {
                Debug.Log($"[InventoryManager] Added: {item.itemName} ({item.rarity}) - Value: {item.value}G");
            }

            // Notificar eventos
            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();

            return true;
        }

        /// <summary>
        /// Remueve un item específico del inventario
        /// </summary>
        public bool RemoveItem(LootItemData item)
        {
            if (collectedItems.Remove(item))
            {
                if (showDebug)
                {
                    Debug.Log($"[InventoryManager] Removed: {item.itemName}");
                }

                OnItemRemoved?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Vacia todo el inventario y retorna el valor total
        /// </summary>
        /// <returns>Valor total de todos los items</returns>
        public int SellAllItems()
        {
            int totalValue = CalculateTotalValue();
            int itemCount = collectedItems.Count;

            collectedItems.Clear();

            if (showDebug)
            {
                Debug.Log($"[InventoryManager] SOLD ALL: {itemCount} items for {totalValue}G");
            }

            OnInventoryChanged?.Invoke();

            return totalValue;
        }

        /// <summary>
        /// Calcula el valor total de todos los items en el inventario
        /// </summary>
        public int CalculateTotalValue()
        {
            int total = 0;
            foreach (var item in collectedItems)
            {
                total += item.value;
            }
            return total;
        }

        /// <summary>
        /// Calcula el peso total del inventario
        /// </summary>
        public float CalculateTotalWeight()
        {
            float total = 0f;
            foreach (var item in collectedItems)
            {
                total += item.weight;
            }
            return total;
        }

        /// <summary>
        /// Obtiene la lista de items (solo lectura)
        /// </summary>
        public List<LootItemData> GetItems()
        {
            return new List<LootItemData>(collectedItems);
        }

        /// <summary>
        /// Número de items en el inventario
        /// </summary>
        public int ItemCount => collectedItems.Count;

        /// <summary>
        /// Verifica si el inventario está lleno
        /// </summary>
        public bool IsFull => maxCapacity > 0 && collectedItems.Count >= maxCapacity;

        /// <summary>
        /// Verifica si el inventario está vacío
        /// </summary>
        public bool IsEmpty => collectedItems.Count == 0;

        /// <summary>
        /// Limpia todo el inventario sin vender
        /// </summary>
        public void Clear()
        {
            collectedItems.Clear();
            OnInventoryChanged?.Invoke();

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Inventory cleared");
            }
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showDebug) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            int yOffset = 120; // Debajo del GameManager debug

            GUI.Label(new Rect(10, yOffset, 300, 20),
                $"Inventory: {ItemCount}{(maxCapacity > 0 ? $"/{maxCapacity}" : "")}", style);

            GUI.Label(new Rect(10, yOffset + 20, 300, 20),
                $"Total Value: {CalculateTotalValue()}G", style);

            GUI.Label(new Rect(10, yOffset + 40, 300, 20),
                $"Total Weight: {CalculateTotalWeight():F1}kg", style);

            // Listar items
            style.fontSize = 12;
            int itemYOffset = yOffset + 65;

            for (int i = 0; i < Mathf.Min(collectedItems.Count, 5); i++)
            {
                var item = collectedItems[i];
                Color rarityColor = item.GetAuraColor();
                style.normal.textColor = rarityColor;

                GUI.Label(new Rect(10, itemYOffset + (i * 15), 300, 15),
                    $"• {item.itemName} ({item.value}G)", style);
            }

            if (collectedItems.Count > 5)
            {
                style.normal.textColor = Color.gray;
                GUI.Label(new Rect(10, itemYOffset + 75, 300, 15),
                    $"... and {collectedItems.Count - 5} more", style);
            }
        }

        #endregion
    }
}