using UnityEngine;
using UnityEngine.UI;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.UI.MainMenu
{
    // este script se encarga de gestionar el menú principal, incluyendo la navegación entre paneles (principal, slots, ajustes, confirmación de borrado) y las acciones correspondientes.
    public class MainMenuController : MonoBehaviour
    {
        [Header("── Paneles ──────────────────────────────────")]
        [SerializeField] private GameObject panelMain;
        [SerializeField] private GameObject panelSlots;
        [SerializeField] private GameObject panelSettings;
        [SerializeField] private GameObject panelConfirmDelete;

        [Header("── Botones del panel principal ─────────────")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnSettings;
        [SerializeField] private Button btnQuit;

        [Header("── Panel de Slots ──────────────────────────")]
        [SerializeField] private SaveSlotUI[] saveSlots;   
        [SerializeField] private Button btnBackFromSlots;

        [Header("── Panel de Settings ───────────────────────")]
        [SerializeField] private SettingsMenuController settingsController;
        [SerializeField] private Button btnBackFromSettings;

        [Header("── Confirmación de borrado ─────────────────")]
        [SerializeField] private Button btnConfirmDelete;
        [SerializeField] private Button btnCancelDelete;

        private int pendingDeleteSlot = -1;

       
        private void Start()
        {
            // Panel principal
            btnPlay.onClick.AddListener(OpenSlots);
            btnSettings.onClick.AddListener(OpenSettings);
            btnQuit.onClick.AddListener(QuitGame);

            // Slots
            btnBackFromSlots.onClick.AddListener(() => ShowPanel(panelMain));
            for (int i = 0; i < saveSlots.Length; i++)
            {
                int idx = i;
                saveSlots[i].OnActionClicked += () => OnSlotSelected(idx);
                saveSlots[i].OnDeleteClicked += () => RequestDelete(idx);
            }

            
            btnBackFromSettings.onClick.AddListener(() => ShowPanel(panelMain));

           
            btnConfirmDelete.onClick.AddListener(ConfirmDelete);
            btnCancelDelete.onClick.AddListener(CancelDelete);

            ShowPanel(panelMain);
        }

        private void ShowPanel(GameObject target)
        {
            panelMain.SetActive(false);
            panelSlots.SetActive(false);
            panelSettings.SetActive(false);
            panelConfirmDelete.SetActive(false);
            target.SetActive(true);
        }

        private void OpenSlots()
        {
            RefreshSlots();
            ShowPanel(panelSlots);
        }

        private void OpenSettings()
        {
            settingsController.RefreshUI();
            ShowPanel(panelSettings);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        
        private void RefreshSlots()
        {
            SaveData[] data = SaveManager.Instance.LoadAllSlots();
            for (int i = 0; i < saveSlots.Length; i++)
                saveSlots[i].Populate(i, data[i], actionLabel: "Jugar");
        }

        private void OnSlotSelected(int slot)
        {
            if (SaveManager.Instance.SlotExists(slot))
                SceneLoader.Instance.LoadGameFromSlot(slot);
            else
                SceneLoader.Instance.StartNewGame(slot);
        }

        private void RequestDelete(int slot)
        {
            pendingDeleteSlot = slot;
            panelConfirmDelete.SetActive(true);
        }

        private void ConfirmDelete()
        {
            if (pendingDeleteSlot >= 0)
            {
                SaveManager.Instance.Delete(pendingDeleteSlot);
                pendingDeleteSlot = -1;
            }
            panelConfirmDelete.SetActive(false);
            RefreshSlots();
        }

        private void CancelDelete()
        {
            pendingDeleteSlot = -1;
            panelConfirmDelete.SetActive(false);
        }
    }
}