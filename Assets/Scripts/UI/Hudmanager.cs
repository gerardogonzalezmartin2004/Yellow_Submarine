using UnityEngine;
using UnityEngine.UI;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    // HUDManager es el controlador central de toda la interfaz.
    // Escucha los cambios de estado del GameController y muestra/oculta
    // el HUD correcto según el modo (Barco o Buceador).
    // Usa CanvasGroup para hacer CrossFade elegante entre los dos modos.
    public class HUDManager : MonoBehaviour
    {
        [Header("HUD Panels")]
        [Tooltip("Panel con todos los elementos del modo Barco")]
        [SerializeField] private CanvasGroup boatHUDGroup;

        [Tooltip("Panel con todos los elementos del modo Buceador")]
        [SerializeField] private CanvasGroup diverHUDGroup;

        [Header("Transition")]
        [Tooltip("Tiempo en segundos del CrossFade al cambiar de modo")]
        [SerializeField] private float fadeTime = 0.4f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        // Estado interno para evitar re-activar el modo si ya está activo
        private GameController.GameState lastState = GameController.GameState.Sailing;
        private bool isFading = false;

        #region Unity ciclo de vida

        private void Start()
        {
            // Nos aseguramos de que ambos paneles existen
            if (boatHUDGroup == null || diverHUDGroup == null)
            {
                Debug.LogError("[HUDManager] Faltan referencias a BoatHUD o DiverHUD");
                return;
            }

            // Estado inicial: mostrar el barco, ocultar el buzo
            boatHUDGroup.alpha = 1f;
            boatHUDGroup.interactable = true;
            boatHUDGroup.blocksRaycasts = true;

            diverHUDGroup.alpha = 0f;
            diverHUDGroup.interactable = false;
            diverHUDGroup.blocksRaycasts = false;

            if (showDebug)
            {
                Debug.Log("[HUDManager] HUD inicializado en modo Barco");
            }
        }

        private void Update()
        {
            // Si no hay GameController, no hacemos nada
            if (GameController.Instance == null)
            {
                return;
            }

            GameController.GameState currentState = GameController.Instance.GetCurrentState();

            // Solo reaccionar si el estado ha cambiado y no estamos ya en transición
            if (currentState != lastState && !isFading)
            {
                OnStateChanged(currentState);
                lastState = currentState;
            }
        }

        #endregion

        #region State Reactions

        private void OnStateChanged(GameController.GameState newState)
        {
            if (showDebug)
            {
                Debug.Log("[HUDManager] Estado cambiado a: " + newState.ToString());
            }

            if (newState == GameController.GameState.Sailing || newState == GameController.GameState.InPort)
            {
                // Modo Barco: activar BoatHUD, desactivar DiverHUD
                StartCoroutine(CrossFadeHUD(boatHUDGroup, diverHUDGroup));
            }
            else if (newState == GameController.GameState.Diving)
            {
                // Modo Buceador: activar DiverHUD, desactivar BoatHUD
                StartCoroutine(CrossFadeHUD(diverHUDGroup, boatHUDGroup));
            }
        }

        #endregion

        #region CrossFade Logic

        // Hace un fade entre dos CanvasGroups de forma suave
        // fadeIn:  el panel que aparece
        // fadeOut: el panel que desaparece
        private System.Collections.IEnumerator CrossFadeHUD(CanvasGroup fadeIn, CanvasGroup fadeOut)
        {
            isFading = true;
            float elapsed = 0f;

            // Preparar el panel que va a aparecer
            fadeIn.gameObject.SetActive(true);
            fadeIn.alpha = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeTime;

                // Ambos se mueven en paralelo: uno aparece, el otro desaparece
                fadeIn.alpha = Mathf.Lerp(0f, 1f, progress);
                fadeOut.alpha = Mathf.Lerp(1f, 0f, progress);

                yield return null;
            }

            // Asegurar valores finales exactos
            fadeIn.alpha = 1f;
            fadeOut.alpha = 0f;

            // Configurar la interactividad correctamente
            fadeIn.interactable = true;
            fadeIn.blocksRaycasts = true;
            fadeOut.interactable = false;
            fadeOut.blocksRaycasts = false;

            // Desactivar el panel oculto para ahorrar rendimiento
            fadeOut.gameObject.SetActive(false);

            isFading = false;

            if (showDebug)
            {
                Debug.Log("[HUDManager] CrossFade completado - activo: " + fadeIn.gameObject.name);
            }
        }

        #endregion

        #region API Pública

        // Permite forzar un modo concreto desde código externo (ej: cutscenes, tienda)
        public void ForceShowBoatHUD()
        {
            if (!isFading)
            {
                StartCoroutine(CrossFadeHUD(boatHUDGroup, diverHUDGroup));
            }
        }

        public void ForceShowDiverHUD()
        {
            if (!isFading)
            {
                StartCoroutine(CrossFadeHUD(diverHUDGroup, boatHUDGroup));
            }
        }

        // Oculta todo el HUD (para menús, cinemáticas, etc.)
        public void HideAll()
        {
            if (boatHUDGroup != null)
            {
                boatHUDGroup.alpha = 0f;
                boatHUDGroup.interactable = false;
            }

            if (diverHUDGroup != null)
            {
                diverHUDGroup.alpha = 0f;
                diverHUDGroup.interactable = false;
            }
        }

        public bool IsFading()
        {
            return isFading;
        }

        #endregion

        #region Debug (GUI)

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            style.normal.textColor = Color.magenta;

            float boatAlpha = boatHUDGroup != null ? boatHUDGroup.alpha : 0f;
            float diverAlpha = diverHUDGroup != null ? diverHUDGroup.alpha : 0f;

            GUI.Label(new Rect(10, 310, 300, 20), "[HUDMgr] BoatHUD: " + boatAlpha.ToString("F2"), style);
            GUI.Label(new Rect(10, 330, 300, 20), "[HUDMgr] DiverHUD: " + diverAlpha.ToString("F2"), style);
            GUI.Label(new Rect(10, 350, 300, 20), "[HUDMgr] Fading: " + isFading, style);
        }

        #endregion
    }
}