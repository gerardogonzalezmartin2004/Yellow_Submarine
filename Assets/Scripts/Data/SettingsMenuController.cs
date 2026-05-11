using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{
    // este script se encarga de gestionar el menú de ajustes, permitiendo modificar el volumen maestro, música y efectos, así como la calidad gráfica. También actualiza las etiquetas de porcentaje y guarda la configuración de calidad en PlayerPrefs.
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
            sliderMaster.minValue = 0f; sliderMaster.maxValue = 1f;
            sliderMusic.minValue = 0f; sliderMusic.maxValue = 1f;
            sliderSFX.minValue = 0f; sliderSFX.maxValue = 1f;

            sliderMaster.onValueChanged.AddListener(v => { AudioManager.Instance.SetMasterVolume(v); UpdateLabels(); });
            sliderMusic.onValueChanged.AddListener(v => { AudioManager.Instance.SetMusicVolume(v); UpdateLabels(); });
            sliderSFX.onValueChanged.AddListener(v => { AudioManager.Instance.SetSFXVolume(v); UpdateLabels(); });

            dropQuality.onValueChanged.AddListener(OnQualityChanged);

            dropQuality.ClearOptions();
            dropQuality.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        }

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