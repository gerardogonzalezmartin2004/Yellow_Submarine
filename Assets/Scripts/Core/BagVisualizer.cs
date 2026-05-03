using UnityEngine;
using AbyssalReach.Core;

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

        private void OnItemAdded(ItemData item)
        {
            if (item == null) return;
            if (currentSlot >= itemSlots.Length) return;

           

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