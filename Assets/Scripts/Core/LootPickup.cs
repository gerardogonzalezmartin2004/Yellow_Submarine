using UnityEngine;
using AbyssalReach.Core;
using UnityEngine.InputSystem;


public class LootPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    private AbyssalReachControls controls;

    private bool playerInRange = false;

    private void Awake()
    {
        controls = new AbyssalReachControls();
    }
    private void OnEnable()
    {
        controls.DiverControls.Interact.Enable();
        controls.DiverControls.Interact.performed += OnInteractPerformed;
    }

    private void OnDisable()
    {
        controls.DiverControls.Interact.performed -= OnInteractPerformed;
        controls.DiverControls.Interact.Disable();
    }
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (!playerInRange) return;
        if (InventoryManager.Instance == null) return;

        bool success = InventoryManager.Instance.TryPickupItem(itemData);
        if (success)
        {
            Destroy(gameObject);
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