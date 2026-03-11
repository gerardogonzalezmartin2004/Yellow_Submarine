using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    public class BagFillVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform visual;

        [SerializeField] private Vector3 emptyScale = Vector3.one;
        [SerializeField] private Vector3 fullScale = new Vector3(1.5f, 1.5f, 1.5f);

        private GridInventory diverInventory;

        private void Start()
        {
            diverInventory = InventoryManager.Instance.GetDiverInventory();

            InventoryManager.OnInventoryChanged += UpdateBag;

            UpdateBag();
        }

        private void OnDestroy()
        {
            InventoryManager.OnInventoryChanged -= UpdateBag;
        }

        private void UpdateBag()
        {
            if (diverInventory == null)
                return;

            float percent = diverInventory.GetCurrentWeight() / diverInventory.GetMaxWeight();

            visual.localScale = Vector3.Lerp(emptyScale, fullScale, percent);
        }
    }
}