using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Controlador para la UI de transferencia Diver → Boat.
    /// Maneja la "staging area" donde el jugador puede reorganizar items
    /// antes de confirmar qué desechar.
    /// </summary>
    public class TransferUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject transferPanel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI diverItemsText;
        [SerializeField] private Button confirmTransferButton;
        [SerializeField] private Button discardRemainingButton;
        [SerializeField] private Button cancelButton;

        [Header("Messages")]
        [SerializeField] private string allTransferredMessage = "¡Todos los items se transfirieron al barco!";
        [SerializeField] private string partialTransferMessage = "Algunos items no cupieron. ¿Deseas descartarlos?";
        [SerializeField] private string confirmDiscardMessage = "¿Seguro que quieres tirar estos items al mar?";

        private int itemsInDiver = 0;

        #region Unity Lifecycle

        private void OnEnable()
        {
            // Suscribirse a eventos
            if (confirmTransferButton != null)
            {
                confirmTransferButton.onClick.AddListener(OnConfirmTransfer);
            }

           

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancel);
            }
        }

        private void OnDisable()
        {
            // Limpiar listeners
            if (confirmTransferButton != null) confirmTransferButton.onClick.RemoveAllListeners();
            if (discardRemainingButton != null) discardRemainingButton.onClick.RemoveAllListeners();
            if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
        }

        #endregion

        #region Public Methods

        
        /// <summary>
        /// Cierra la UI de transferencia
        /// </summary>
        public void CloseTransferUI()
        {
            if (transferPanel != null)
            {
                transferPanel.SetActive(false);
            }
        }

        #endregion

        #region Transfer Logic

        

        /// <summary>
        /// Actualiza los textos de la UI según el estado actual
        /// </summary>
        private void UpdateDisplay()
        {
            // Actualizar contador de items restantes
            if (diverItemsText != null)
            {
                diverItemsText.text = "Items en staging area: " + itemsInDiver;
            }

            // Actualizar mensaje de estado
            if (statusText != null)
            {
                if (itemsInDiver == 0)
                {
                    statusText.text = allTransferredMessage;
                }
                else
                {
                    statusText.text = partialTransferMessage;
                }
            }

            // Mostrar/ocultar botón de desechar
            if (discardRemainingButton != null)
            {
                discardRemainingButton.gameObject.SetActive(itemsInDiver > 0);
            }

            // Si no hay items restantes, el botón de confirmar cierra directamente
            if (confirmTransferButton != null)
            {
                TextMeshProUGUI buttonText = confirmTransferButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = (itemsInDiver > 0) ? "Reorganizar" : "Continuar";
                }
            }
        }

        #endregion

        #region Button Callbacks

        /// <summary>
        /// Botón "Confirmar/Continuar"
        /// Si no hay items restantes, cierra la UI
        /// Si hay items, abre la UI de reorganización (TU UI CUSTOM)
        /// </summary>
        private void OnConfirmTransfer()
        {
            if (itemsInDiver == 0)
            {
                // No hay items restantes, simplemente cerrar
                CloseTransferUI();
            }
            else
            {
                // Hay items en staging area - aquí abres TU UI CUSTOM de reorganización
                // TODO: Llamar a tu sistema de UI de inventario visual
                Debug.Log("[TransferUI] Abriendo UI de reorganización (implementar tu Canvas aquí)");

                // Por ahora cerramos esta UI
                CloseTransferUI();

                // Aquí llamarías algo como:
                // InventoryGridUI.Instance.OpenStagingArea();
            }
        }

        

        /// <summary>
        /// Botón "Cancelar"
        /// Cierra la UI sin hacer cambios
        /// </summary>
        private void OnCancel()
        {
            CloseTransferUI();
        }

        #endregion

        
    }
}