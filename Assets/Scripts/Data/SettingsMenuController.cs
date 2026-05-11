using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Panel de ajustes reutilizable. Lo puedes poner tanto en el Menú Principal
    /// como en la Pausa — simplemente arrástralo y llama RefreshUI() al abrirlo.
    /// </summary>
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Sliders de volumen")]
        [SerializeField] private Slider sliderMaster;
        [SerializeField] private Slider sliderMusic;
        [SerializeField] private Slider sliderSFX;

        [Header("Labels de porcentaje")]
        [SerializeField] private TextMeshProUGUI lblMaster;
        [SerializeField] private TextMeshProUGUI lblMusic;
        [SerializeField] private TextMeshProUGUI lblSFX;

        [Header("Calidad gráfica")]
        [SerializeField] private TMP_Dropdown dropQuality;

        private void Start()
        {
            // Rangos 0–1
            sliderMaster.minValue = 0f; sliderMaster.maxValue = 1f;
            sliderMusic.minValue = 0f; sliderMusic.maxValue = 1f;
            sliderSFX.minValue = 0f; sliderSFX.maxValue = 1f;

            // Listeners
            sliderMaster.onValueChanged.AddListener(v => { AudioManager.Instance.SetMasterVolume(v); UpdateLabels(); });
            sliderMusic.onValueChanged.AddListener(v => { AudioManager.Instance.SetMusicVolume(v); UpdateLabels(); });
            sliderSFX.onValueChanged.AddListener(v => { AudioManager.Instance.SetSFXVolume(v); UpdateLabels(); });

            dropQuality.onValueChanged.AddListener(OnQualityChanged);

            // Rellena opciones de calidad según Unity Quality Settings
            dropQuality.ClearOptions();
            dropQuality.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        }

        // ── Llamar al abrir el panel para sincronizar la UI con los valores reales ──
        public void RefreshUI()
        {
            if (AudioManager.Instance == null) return;

            sliderMaster.SetValueWithoutNotify(AudioManager.Instance.GetMasterVolume());
            sliderMusic.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
            sliderSFX.SetValueWithoutNotify(AudioManager.Instance.GetSFXVolume());
            dropQuality.SetValueWithoutNotify(QualitySettings.GetQualityLevel());

            UpdateLabels();
        }

        private void OnQualityChanged(int level)
        {
            QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true);
            PlayerPrefs.SetInt("quality_level", level);
        }

        private void UpdateLabels()
        {
            if (lblMaster) lblMaster.text = $"{Mathf.RoundToInt(sliderMaster.value * 100)}%";
            if (lblMusic) lblMusic.text = $"{Mathf.RoundToInt(sliderMusic.value * 100)}%";
            if (lblSFX) lblSFX.text = $"{Mathf.RoundToInt(sliderSFX.value * 100)}%";
        }
    }
}