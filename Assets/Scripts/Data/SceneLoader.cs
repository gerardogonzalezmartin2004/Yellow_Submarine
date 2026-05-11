using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Singleton persistente. Gestiona transiciones entre escenas de forma asíncrona
    /// y comunica al GameInitializer qué slot cargar al entrar en la escena de juego.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;
        public static SceneLoader Instance => instance;

        // ── Nombres de escena — deben coincidir con Build Settings ─────────────
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAME = "GameScene"; // ← cambia por tu nombre real

        [Header("Loading Screen (opcional)")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;

        // ── Estado compartido ─────────────────────────────────────────────────
        public int PendingSlot { get; private set; } = -1;
        public bool IsNewGame { get; private set; } = true;

        // ─── Singleton ────────────────────────────────────────────────────────
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ─── API pública ──────────────────────────────────────────────────────
        public void StartNewGame(int slot)
        {
            PendingSlot = slot;
            IsNewGame = true;
            StartCoroutine(LoadAsync(SCENE_GAME));
        }

        public void LoadGameFromSlot(int slot)
        {
            PendingSlot = slot;
            IsNewGame = false;
            StartCoroutine(LoadAsync(SCENE_GAME));
        }

        public void GoToMainMenu()
        {
            PendingSlot = -1;
            Time.timeScale = 1f; // por si venimos de pausa
            StartCoroutine(LoadAsync(SCENE_MAIN_MENU));
        }

        // ─── Carga asíncrona ──────────────────────────────────────────────────
        private IEnumerator LoadAsync(string sceneName)
        {
            if (loadingScreen != null) loadingScreen.SetActive(true);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                if (loadingBar != null) loadingBar.value = op.progress;
                if (loadingText != null) loadingText.text = $"Cargando... {Mathf.RoundToInt(op.progress * 100)}%";
                yield return null;
            }

            // Pequeño delay para que la pantalla de carga no parpadee
            yield return new WaitForSecondsRealtime(0.4f);

            op.allowSceneActivation = true;

            if (loadingScreen != null) loadingScreen.SetActive(false);
        }
    }
}