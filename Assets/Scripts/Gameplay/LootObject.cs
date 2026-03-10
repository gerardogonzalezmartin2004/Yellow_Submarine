using UnityEngine;
using AbyssalReach.Data;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    /// <summary>
    /// Script para objetos recogibles en el mundo.
    /// Se conecta con InteractablePrompt2D para manejar la interacción del jugador.
    /// </summary>
    public class LootObject : MonoBehaviour
    {
        [Header("Loot Configuration")]
        [Tooltip("Los datos del item que contiene este objeto")]
        [SerializeField] private LootItemData lootData;

        [Header("Visual Feedback")]
        [Tooltip("Partículas al recoger (opcional)")]
        [SerializeField] private ParticleSystem pickupParticles;

        [Tooltip("Audio al recoger (opcional)")]
        [SerializeField] private AudioClip pickupSound;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        #region Interaction

        /// <summary>
        /// Método público que InteractablePrompt2D llamará cuando el jugador pulse E/Botón
        /// IMPORTANTE: Este método debe ser arrastrado al evento OnInteract del InteractablePrompt2D
        /// </summary>
        public void Interact()
        {
            if (lootData == null)
            {
                Debug.LogError("[LootObject] No hay LootItemData asignado en " + gameObject.name);
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[LootObject] InventoryManager no encontrado");
                return;
            }

            // Intentar recoger el item
            bool success = InventoryManager.Instance.TryPickupItem(lootData);

            if (success)
            {
                if (showDebug)
                {
                    Debug.Log("[LootObject] Recogido: " + lootData.itemName);
                }

                // Feedback visual/audio
                OnPickupSuccess();

                // Destruir el objeto del mundo
                Destroy(gameObject);
            }
            else
            {
                if (showDebug)
                {
                    Debug.LogWarning("[LootObject] No se pudo recoger " + lootData.itemName + " (Inventario lleno o demasiado pesado)");
                }

                // Feedback de error (opcional)
                OnPickupFailed();
            }
        }

        #endregion

        #region Feedback

        /// <summary>
        /// Se ejecuta cuando el item se recogió exitosamente
        /// </summary>
        private void OnPickupSuccess()
        {
            // Reproducir partículas
            if (pickupParticles != null)
            {
                // Crear una instancia temporal de las partículas
                ParticleSystem particles = Instantiate(pickupParticles, transform.position, Quaternion.identity);
                Destroy(particles.gameObject, particles.main.duration + 1f);
            }

            // Reproducir sonido
            if (pickupSound != null)
            {
                // Crear un GameObject temporal para reproducir el audio
                GameObject audioObj = new GameObject("PickupSound");
                audioObj.transform.position = transform.position;
                AudioSource audioSource = audioObj.AddComponent<AudioSource>();
                audioSource.clip = pickupSound;
                audioSource.Play();
                Destroy(audioObj, pickupSound.length + 0.5f);
            }
        }

        /// <summary>
        /// Se ejecuta cuando el item NO se pudo recoger
        /// Aquí puedes añadir feedback visual/audio de error
        /// </summary>
        private void OnPickupFailed()
        {
            // TODO: Añadir feedback de error (ej: shake del objeto, sonido de error)
            // Por ahora solo dejamos el Debug.LogWarning que está arriba
        }

        #endregion

        #region Editor Helpers

        private void OnValidate()
        {
            // Auto-nombrar el GameObject según el item
            if (lootData != null && string.IsNullOrEmpty(gameObject.name) == false)
            {
                if (gameObject.name.StartsWith("LootObject") || gameObject.name == "GameObject")
                {
                    gameObject.name = "Loot_" + lootData.itemName;
                }
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (lootData == null) return;

            // Dibujar un ícono en el editor para visualizar el loot
            Gizmos.color = lootData.GetAuraColor();
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        private void OnDrawGizmosSelected()
        {
            if (lootData == null) return;

            // Mostrar información del item cuando está seleccionado
            Gizmos.color = lootData.GetAuraColor();
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        #endregion
    }
}