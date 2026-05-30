using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.UI.Pause
{
    // este script se encarga de gestionar el menú de pausa, incluyendo la navegación entre paneles (pausa, ajustes, guardar) y las acciones correspondientes.
    public class PauseMenuController : MonoBehaviour
    {
        [Header("── Paneles ──────────────────────────────────")]
        [SerializeField] private GameObject panelPause;
        [SerializeField] private GameObject panelSettings;
        [SerializeField] private GameObject panelSave;

        [Header("── Botones del panel de pausa ──────────────")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnSave;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnMainMenu;

        [Header("── Panel Settings ──────────────────────────")]
        [SerializeField] private SettingsMenuController settingsController;
        [SerializeField] private Button btnBackFromSettings;

        [Header("── Panel Guardar ───────────────────────────")]
        [SerializeField] private SaveSlotUI[] saveSlots;
        [SerializeField] private Button btnBackFromSave;

        private bool isPaused = false;
        private GameController.GameState stateBeforePause;
        private AbyssalReachControls controls;

       
        private void Start()
        {
            
            btnResume.onClick.AddListener(ResumeGame);
            btnSave.onClick.AddListener(OpenSave);
            btnSettings.onClick.AddListener(OpenSettings);
            btnMainMenu.onClick.AddListener(GoMainMenu);
            btnBackFromSettings.onClick.AddListener(() => ShowPanel(panelPause));
            btnBackFromSave.onClick.AddListener(() => ShowPanel(panelPause));

            for (int i = 0; i < saveSlots.Length; i++)
            {
                if (saveSlots[i] == null) continue;
                int idx = i;
                saveSlots[i].OnActionClicked += () => SaveToSlot(idx);
            }

            HideAll();
            controls = GameController.Instance?.GetControls();
            if (controls != null)
            {
                controls.Global.Pause.performed += OnPauseInput;
                Debug.Log("[PauseMenu] Suscrito a Global.Pause, CI");
            }
            else
            {
                Debug.LogError("[PauseMenu] No se encontró GameController.Instance");
            }
        }


        private void OnDisable()
        {
            if (controls == null) return;
            controls.Global.Pause.performed -= OnPauseInput;
        }


        private void OnPauseInput(InputAction.CallbackContext ctx)
        {
            if (GameController.Instance != null)
            {
                GameController.GameState state = GameController.Instance.GetCurrentState();
                if (state == GameController.GameState.InShop)
                    return;
            }
            TogglePause();
        }

        public void TogglePause()
        {
            AudioManager.Instance.PlaySFX("Button1");
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        public void ButtonSound()
        {
            AudioManager.Instance.PlaySFX("Button1");
        }

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;

            stateBeforePause = GameController.Instance.GetCurrentState();
            GameController.Instance.SetGameState(GameController.GameState.Paused);

            Time.timeScale = 0f;
            ShowPanel(panelPause);

            Debug.Log("[PauseMenu] Juego pausado.");
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;

            GameController.Instance.SetGameState(stateBeforePause);
            Time.timeScale = 1f;
            HideAll();

            Debug.Log("[PauseMenu] Juego reanudado.");
        }

        private void ShowPanel(GameObject target)
        {
            panelPause.SetActive(false);
            panelSettings.SetActive(false);
            panelSave.SetActive(false);
            target.SetActive(true);
        }

        private void HideAll()
        {
            panelPause.SetActive(false);
            panelSettings.SetActive(false);
            panelSave.SetActive(false);
        }

        private void OpenSettings()
        {
            settingsController.RefreshUI();
            ShowPanel(panelSettings);
        }

        private void OpenSave()
        {
            RefreshSaveSlots();
            ShowPanel(panelSave);
        }

        private void GoMainMenu() => SceneLoader.Instance.GoToMainMenu();

        private void RefreshSaveSlots()
        {
            SaveData[] data = SaveManager.Instance.LoadAllSlots();
            for (int i = 0; i < saveSlots.Length; i++)
            {
                if (saveSlots[i] == null) continue;
                saveSlots[i].Populate(i, data[i], actionLabel: "Guardar aquí");
            }
        }

        private void SaveToSlot(int slot)
        {
            if (GameInitializer.Instance == null)
            {
                Debug.LogError("[PauseMenu] GameInitializer no encontrado.");
                return;
            }

            SaveData data = GameInitializer.Instance.CollectSaveData();
            data.slotIndex = slot;
            SaveManager.Instance.Save(slot, data);
            RefreshSaveSlots();

            Debug.Log($"[PauseMenu] Partida guardada en slot {slot}.");
        }
    }
}