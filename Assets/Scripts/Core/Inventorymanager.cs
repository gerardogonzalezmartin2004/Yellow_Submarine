using System.Collections.Generic;
using UnityEngine;

namespace AbyssalReach.Core
{
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager instance;
        public static InventoryManager Instance => instance;

        [SerializeField] private bool showDebug = true;

        private DiverInventory diverInventory;

        public delegate void InventoryChanged();
        public static event InventoryChanged OnInventoryChanged;

        public delegate void ItemAdded(ItemData item);
        public static event ItemAdded OnItemAdded;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            diverInventory = new DiverInventory();

            if (showDebug)
                Debug.Log("[InventoryManager] Inicializado correctamente.");
        }

        public bool TryPickupItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[InventoryManager] TryPickupItem: item es null");
                return false;
            }

            string error;
            bool success = diverInventory.TryAddItem(item, out error);

            if (success)
            {
                OnInventoryChanged?.Invoke();
                OnItemAdded?.Invoke(item);
                if (showDebug)
                    Debug.Log("[InventoryManager] Recogido: " + item.name +
                              " | Total items: " + diverInventory.GetItemCount());
            }
            else
            {
                Debug.LogWarning("[InventoryManager] No se pudo recoger " + item.name + ": " + error);
            }

            return success;
        }

        public DiverInventory GetDiverInventory() => diverInventory;

        public void DiscardDiverItems()
        {
            diverInventory.Clear();
            OnInventoryChanged?.Invoke();
        }

        // Método público para que otros scripts puedan pedirle al Manager que actualice la UI
        public void NotifyInventoryUpdate()
        {
            OnInventoryChanged?.Invoke();
        }
        public int CalculateTotalValue()
        {
            int totalValue = 0;

            foreach (ItemData item in diverInventory.GetItems())
            {
                if (item != null)
                    totalValue += item.value;
            }

            return totalValue;
        }

        public int SellAllItems()
        {
            int totalValue = CalculateTotalValue();

            if (totalValue > 0)
            {
                diverInventory.Clear();
                OnInventoryChanged?.Invoke();

                if (showDebug)
                    Debug.Log("[InventoryManager] Vendidos todos los items por " + totalValue + "G");
            }

            return totalValue;
        }

        public int GetItemCount() => diverInventory.GetItemCount();
        public bool IsEmpty() => diverInventory.GetItemCount() == 0;

        private void OnGUI()
        {
            if (!showDebug) return;

            GUIStyle style = new GUIStyle { fontSize = 14 };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 120, 300, 20),
                "Diver: " + diverInventory.GetItemCount() + " items / " +
                diverInventory.GetCurrentWeight().ToString("F1") + "kg", style);
        }
    }
}