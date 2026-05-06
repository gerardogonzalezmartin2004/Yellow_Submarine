using UnityEngine;
using AbyssalReach.Core;

// Ponlo en cualquier objeto 2D con Collider2D marcado como Trigger.
public class OxygenDrainer : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Oxígeno que quita por segundo mientras el diver esté dentro")]
    [SerializeField] private float oxygenDrainPerSecond = 10f;

    [Tooltip("True = drena continuamente. False = drena solo al entrar")]
    [SerializeField] private bool continuousDrain = true;

    [Tooltip("Mensaje opcional en pantalla (déjalo vacío para no mostrar nada)")]
    [SerializeField] private string warningMessage = "¡Corriente peligrosa!";

    private bool diverInside = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Diver")) return;

        diverInside = true;

        if (!string.IsNullOrEmpty(warningMessage))
            Debug.Log($"[OxygenDrainer] {warningMessage}");

        // Si no es continuo, drena de golpe al entrar
        if (!continuousDrain)
            GameController.Instance?.DrainOxygen(oxygenDrainPerSecond);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Diver"))
            diverInside = false;
    }

    private void Update()
    {
        if (!continuousDrain || !diverInside) return;

        GameController.Instance?.DrainOxygen(oxygenDrainPerSecond * Time.deltaTime);
    }
}