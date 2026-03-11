using UnityEngine;
using AbyssalReach.Core;
using AbyssalReach.Data;

public class LootPickup : MonoBehaviour
{
    [SerializeField] private LootItemData itemData;

    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    private bool playerInRange = false;

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(pickupKey))
        {
            bool success = InventoryManager.Instance.TryPickupItem(itemData);

            if (success)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Diver"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Diver"))
        {
            playerInRange = false;
        }
    }
}