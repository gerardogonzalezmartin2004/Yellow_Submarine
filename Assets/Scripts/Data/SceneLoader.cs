using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace AbyssalReach.Core
{
    // este script se encarga de gestionar la carga de escenas, incluyendo la transición entre el menú principal y el juego, así como la pantalla de carga opcional con barra de progreso y texto. También mantiene información sobre el slot pendiente y si es una nueva partida o no.
    public class SceneLoader : MonoBehaviour
    {
        private static SceneLoader instance;
        public static SceneLoader Instance => instance;

        
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_GAME = "GameScene"; 

        [Header("Loading Screen (opcional)")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;

      
        public int PendingSlot { get; private set; } = -1;
        public bool IsNewGame { get; private set; } = true;

       
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

       
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
            Time.timeScale = 1f; 
            StartCoroutine(LoadAsync(SCENE_MAIN_MENU));
        }

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

            yield return new WaitForSecondsRealtime(0.4f);

            op.allowSceneActivation = true;

            if (loadingScreen != null) loadingScreen.SetActive(false);
        }
    }
}