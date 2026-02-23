using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    // Sistema centralizado de navegación de UI con eventos para HUD
    public class UINavigationManager : MonoBehaviour
    {
        // Singleton optimizado
        public static UINavigationManager Instance 
        { 
            get;
            private set; 
        }

        // Evento  usando System.Action
        public static event Action<bool> OnShopStateChanged;

        [Header("HUD Management")]
        [Tooltip("Canvas del HUD que se oculta al abrir la tienda")]
        [SerializeField] private CanvasGroup hudCanvasGroup;

        [Tooltip("Duración del fade del HUD (segundos)")]
        [SerializeField] private float hudFadeDuration = 0.3f;

        [Header("Flicker Fix")]
        [Tooltip("Frames de espera antes de activar el panel (previene flicker)")]
        [SerializeField] private int frameDelayBeforeActivation = 2;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        // Caché del último botón seleccionado para cada panel
        private Dictionary<GameObject, GameObject> panelLastSelected = new Dictionary<GameObject, GameObject>();

        // Coroutine de fade del HUD
        private Coroutine hudFadeCoroutine;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (hudCanvasGroup == null)
            {
                Debug.LogWarning("[UINav] HUD Canvas Group no asignado - el HUD no se ocultará.");
            }
        }

        #endregion

        #region Public API

        // Abre un panel de UI (menú/tienda), oculta el HUD y establece el botón inicial seleccionado.
        public void OpenPanel(GameObject panel, GameObject firstButton)
        {
            if (panel == null || firstButton == null)
            {
                Debug.LogError("[UINav] Panel o firstButton es null");
                return;
            }
                        
            StartCoroutine(OpenPanelDelayed(panel, firstButton));
        }

        //Cierra un panel de UI, restaura el HUD y devuelve el control al jugador.
        public void ClosePanel(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

          
            ClearSelection();
            panel.SetActive(false);

            // Restaurar Input Map a Gameplay
            if (GameController.Instance != null)
            {
                GameController.Instance.SetInputToGameplay();
                //Tb se puede poner: GameController.Instance?.SetInputToGameplay();
            }

            ShowHUD();
            OnShopStateChanged?.Invoke(false);
        }

        // Fuerza la selección de un botón específico en el EventSystem
        public void SetSelectedButton(GameObject button)
        {
            ClearSelection();
            StartCoroutine(SelectNextFrame(button));
        }

        // Limpiar selección
        public void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        #endregion

        #region HUD Management

        // Ocultar HUD con fade
        private void HideHUD()
        {
            if (hudCanvasGroup == null)
            {
                return;
            }

            // Cancelar fade anterior si existe
            if (hudFadeCoroutine != null)
            {
                StopCoroutine(hudFadeCoroutine);
            }

            hudFadeCoroutine = StartCoroutine(FadeHUD(0f));

            if (showDebug)
            {
                Debug.Log("[UINav] Ocultando HUD");
            }
        }

        // Mostrar HUD con fade
        private void ShowHUD()
        {
            if (hudCanvasGroup == null)
            {
                return;
            }

            // Cancelar fade anterior si existe
            if (hudFadeCoroutine != null)
            {
                StopCoroutine(hudFadeCoroutine);
            }

            hudFadeCoroutine = StartCoroutine(FadeHUD(1f));

           
        }

        // Coroutine de fade
        private IEnumerator FadeHUD(float targetAlpha)
        {
            if (hudCanvasGroup == null)
            {
                yield break;
            }

            float startAlpha = hudCanvasGroup.alpha;
            float elapsed = 0f;
            // Mientras el fade no haya terminado, interpolar el alpha
            while (elapsed < hudFadeDuration)
            {
                elapsed += Time.deltaTime;
                hudCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / hudFadeDuration);
                yield return null;
            }

            hudCanvasGroup.alpha = targetAlpha;

            // Desactivar interacción si el HUD está oculto 
            bool isVisible = targetAlpha > 0.5f;
            hudCanvasGroup.interactable = isVisible;
            hudCanvasGroup.blocksRaycasts = isVisible;
        }

        #endregion

        #region Internal Coroutines

        private IEnumerator OpenPanelDelayed(GameObject panel, GameObject firstButton)
        {
            // Esperar frames x antes de activar
            for (int i = 0; i < frameDelayBeforeActivation; i = i + 1)
            {
                yield return null;
            }

            // Activar el panel
            if (!panel.activeSelf)
            {
                panel.SetActive(true);
            }

            // Cambiar Input a UI 
            if (GameController.Instance != null)
            {
                GameController.Instance.SetInputToUI();
            }

            // Establecer el primer botón seleccionado
            SetSelectedButton(firstButton);

            // Guardar en caché
            if (!panelLastSelected.ContainsKey(panel))
            {
                panelLastSelected.Add(panel, firstButton);
            }
            else
            {
                panelLastSelected[panel] = firstButton;
            }

            // Ocultar HUD con fade
            HideHUD();

            // Disparar evento
            if (OnShopStateChanged != null)
            {
                OnShopStateChanged(true);
                //OnShopStateChanged?.Invoke(true), otra mas corta
            }

           
        }

        #endregion

        #region Helper Methods

        private System.Collections.IEnumerator SelectNextFrame(GameObject button)
        {
            yield return null;

            if (EventSystem.current != null && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button);

                if (showDebug)
                {
                    Debug.Log("[UINav] Botón seleccionado: " + button.name);
                }
            }
        }      

        #endregion
    }
}