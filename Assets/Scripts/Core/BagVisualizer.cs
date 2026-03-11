using UnityEngine;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.Gameplay
{
    public class BagVisualizer : MonoBehaviour
    {
        [Header("Bag Slots")]
        [SerializeField] private Transform[] itemSlots;

        private int currentSlot = 0;

        private void OnEnable()
        {
            InventoryManager.OnItemAdded += OnItemAdded;
        }

        private void OnDisable()
        {
            InventoryManager.OnItemAdded -= OnItemAdded;
        }

        private void OnItemAdded(LootItemData item)
        {
            if (currentSlot >= itemSlots.Length)
                return;

            if (item.worldPrefab != null)
            {
                Instantiate(item.worldPrefab, itemSlots[currentSlot]);
            }

            currentSlot++;
        }

        public void ClearBagVisuals()
        {
            foreach (Transform slot in itemSlots)
            {
                foreach (Transform child in slot)
                {
                    Destroy(child.gameObject);
                }
            }

            currentSlot = 0;
        }
    }
}