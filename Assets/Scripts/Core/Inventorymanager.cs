using System.Collections.Generic;
using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    // Este script es el "Inventario" del jugador.
    // Guarda una lista de los tesoros LootItemData que has recogido pero aún no has vendido.
    // Es un Singleton para poder acceder a él desde cualquier sitio 
    public class InventoryManager : MonoBehaviour
    {
        // Instancia estática privada
        private static InventoryManager instance;

        [Header("Current Inventory")]
        [Tooltip("Lista de objetos que llevas actualmente")]
        [SerializeField] private List<LootItemData> collectedItems = new List<LootItemData>();

        [Header("Capacity")]
        [Tooltip("Máximo número de items que puede llevar (0 = ilimitado)")]
        [SerializeField] private int maxCapacity = 0;

        [Header("Debug")]
        [Tooltip("Muestra información en pantalla si es true")]
        [SerializeField] private bool showDebug = true;

        #region Events

        // delegate y eventos para notificar a la UI cuando algo cambia
        public delegate void InventoryChanged();
        public static event InventoryChanged OnInventoryChanged;

        public delegate void ItemAdded(LootItemData item);
        public static event ItemAdded OnItemAdded;

        public delegate void ItemRemoved(LootItemData item);
        public static event ItemRemoved OnItemRemoved;

        #endregion

        #region Singleton

        // Getter público para acceder a la instancia
        public static InventoryManager Instance
        {
            get
            {
                return instance;
            }
        }

        private void Awake()
        {
            // Configuración del Singleton
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Aplicaciones

        // Intenta añadir un item al inventario.
        // Devuelve true si tuvo éxito, false si el inventario estaba lleno.
        public bool AddItem(LootItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[InventoryManager] Se intentó añadir un item nulo");
                return false;
            }

            // Verificar si tenemos espacio (si maxCapacity es mayor que 0)
            if (maxCapacity > 0)
            {
                if (collectedItems.Count >= maxCapacity)
                {
                    if (showDebug)
                    {
                        Debug.LogWarning("[InventoryManager] Inventario lleno! (" + collectedItems.Count + "/" + maxCapacity + ")");
                    }
                    return false;
                }
            }

            // Añadir el item a la lista
            collectedItems.Add(item);

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Añadido: " + item.itemName + " (" + item.rarity + ") - Valor: " + item.value + "G");
            }

            // Notificar eventos 
            if (OnItemAdded != null)
            {
                OnItemAdded.Invoke(item);
            }

            if (OnInventoryChanged != null)
            {
                OnInventoryChanged.Invoke();
            }

            return true;
        }

        // Elimina un item específico 
        public bool RemoveItem(LootItemData item)
        {
            if (collectedItems.Remove(item))
            {
                if (showDebug)
                {
                    Debug.Log("[InventoryManager] Eliminado: " + item.itemName);
                }

                if (OnItemRemoved != null)
                {
                    OnItemRemoved.Invoke(item);
                }

                if (OnInventoryChanged != null)
                {
                    OnInventoryChanged.Invoke();
                }

                return true;
            }

            return false;
        }

        // Vende todo el inventario, limpia la lista y devuelve el oro total ganado
        public int SellAllItems()
        {
            int totalValue = CalculateTotalValue();
            int itemCount = collectedItems.Count;

            // Limpiamos la lista
            collectedItems.Clear();

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Vendido todo: " + itemCount + " items por " + totalValue + "G");
            }

            if (OnInventoryChanged != null)
            {
                OnInventoryChanged.Invoke();
            }

            return totalValue;
        }

        // Calcula cuánto vale todo lo que llevas encima
        public int CalculateTotalValue()
        {
            int total = 0;

            // Recorremos la lista sumando el valor de cada item
            foreach (LootItemData item in collectedItems)
            {
                total = total + item.value;
            }

            return total;
        }

        // Calcula cuánto pesa todo lo que llevas encima 
        public float CalculateTotalWeight()
        {
            float total = 0f;

            foreach (LootItemData item in collectedItems)
            {
                total = total + item.weight;
            }

            return total;
        }

        // Devuelve una copia de la lista de items
        public List<LootItemData> GetItems()
        {
            return new List<LootItemData>(collectedItems);
        }

        // Getters 
        public int GetItemCount()
        {
            return collectedItems.Count;
        }

        public bool IsFull()
        {
            if (maxCapacity > 0 && collectedItems.Count >= maxCapacity)
            {
                return true;
            }
            return false;
        }

        public bool IsEmpty()
        {
            if (collectedItems.Count == 0)
            {
                return true;
            }
            return false;
        }

        public void Clear()
        {
            collectedItems.Clear();

            if (OnInventoryChanged != null)
            {
                OnInventoryChanged.Invoke();
            }

            if (showDebug)
            {
                Debug.Log("[InventoryManager] Inventario limpiado");
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            // Posición vertical base para dibujar
            int yOffset = 120; // Debajo del GameManager debug

            // Construir string de capacidad
            string capacityText = "";
            if (maxCapacity > 0)
            {
                capacityText = "/" + maxCapacity;
            }

            // Dibujar contadores generales
            GUI.Label(new Rect(10, yOffset, 300, 20), "Inventory: " + GetItemCount() + capacityText, style);
            GUI.Label(new Rect(10, yOffset + 20, 300, 20), "Total Value: " + CalculateTotalValue() + "G", style);
            // "F1" formatea el float a 1 decimal
            GUI.Label(new Rect(10, yOffset + 40, 300, 20), "Total Weight: " + CalculateTotalWeight().ToString("F1") + "kg", style);

            // Listar los primeros 5 items en pantalla
            style.fontSize = 12;
            int itemYOffset = yOffset + 65;
            int itemsToShow = Mathf.Min(collectedItems.Count, 5);

            for (int i = 0; i < itemsToShow; i++)
            {
                LootItemData item = collectedItems[i];

                // Usamos el color de rareza del item para el texto
                Color rarityColor = item.GetAuraColor();
                style.normal.textColor = rarityColor;

                GUI.Label(new Rect(10, itemYOffset + (i * 15), 300, 15), "• " + item.itemName + " (" + item.value + "G)", style);
            }

            // Si hay más de 5, mostrar aviso
            if (collectedItems.Count > 5)
            {
                style.normal.textColor = Color.gray;
                int remaining = collectedItems.Count - 5;
                GUI.Label(new Rect(10, itemYOffset + 75, 300, 15), "... and " + remaining + " more", style);
            }
        }

        #endregion
    }
}