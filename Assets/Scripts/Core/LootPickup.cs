using UnityEngine;
using AbyssalReach.Core;

// El buzo recoge el objeto SOLO al pulsar E cuando está en rango.
// El objeto se desactiva (no destruye) para poder reaparecer si se descarta.
public class LootPickup : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;

    [Header("Settings")]
    [SerializeField] private string diverTag = "Diver";
    [SerializeField] private KeyCode pickupKey = KeyCode.E;

    [Header("UI Prompt (opcional)")]
    [Tooltip("GameObject con el texto '[E] Recoger' — se activa al entrar en rango.")]
    [SerializeField] private GameObject pickupPrompt;

    private bool diverInRange = false;
    private bool alreadyPickedUp = false;

    #region Trigger Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (alreadyPickedUp) return;
        if (!other.CompareTag(diverTag)) return;

        diverInRange = true;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(diverTag)) return;

        diverInRange = false;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    #endregion

    #region Update — esperar input

    private void Update()
    {
        if (!diverInRange || alreadyPickedUp) return;

        if (Input.GetKeyDown(pickupKey))
            TryPickup();
    }

    #endregion

    #region Pickup Logic

    private void TryPickup()
    {
        if (itemData == null)
        {
            Debug.LogWarning("[LootPickup] itemData no asignado en " + gameObject.name);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[LootPickup] InventoryManager.Instance es null");
            return;
        }

        bool picked = InventoryManager.Instance.TryPickupItem(itemData, gameObject);

        if (picked)
        {
            alreadyPickedUp = true;
            diverInRange = false;

            if (pickupPrompt != null)
                pickupPrompt.SetActive(false);

            // Desactivar en vez de destruir — DropZoneGrid lo reactivará si se descarta.
            gameObject.SetActive(false);

            Debug.Log($"[LootPickup] Recogido: {itemData.itemName}");
        }
        else
        {
            Debug.LogWarning($"[LootPickup] No se pudo recoger {itemData.itemName} (peso máximo?)");
        }
    }

    // Llamado por DropZoneGrid antes de reactivar el objeto.
    public void ResetForReuse()
    {
        alreadyPickedUp = false;
        diverInRange = false;

        if (pickupPrompt != null)
            pickupPrompt.SetActive(false);
    }

    #endregion
}