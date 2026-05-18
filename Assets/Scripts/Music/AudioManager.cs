using UnityEngine;
using UnityEngine.Audio;

namespace AbyssalReach.Core
{
    // este script se encarga de gestionar el audio del juego, incluyendo el volumen maestro, música y efectos, aplicando los cambios al AudioMixer y guardando las preferencias en PlayerPrefs para que persistan entre sesiones. También implementa el patrón singleton para facilitar el acceso desde otros scripts.
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        private const string PREF_MASTER = "vol_master";
        private const string PREF_MUSIC = "vol_music";
        private const string PREF_SFX = "vol_sfx";

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => LoadAndApplySaved();

       
        public void SetMasterVolume(float v) { ApplyToMixer(PARAM_MASTER, v); PlayerPrefs.SetFloat(PREF_MASTER, v); }
        public void SetMusicVolume(float v) { ApplyToMixer(PARAM_MUSIC, v); PlayerPrefs.SetFloat(PREF_MUSIC, v); }
        public void SetSFXVolume(float v) { ApplyToMixer(PARAM_SFX, v); PlayerPrefs.SetFloat(PREF_SFX, v); }

       
        public float GetMasterVolume() => PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        public float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        public float GetSFXVolume() => PlayerPrefs.GetFloat(PREF_SFX, 1f);

       
        private void ApplyToMixer(string param, float linear)
        {
            float dB = linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
            masterMixer.SetFloat(param, dB);
        }

        private void LoadAndApplySaved()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(PREF_MASTER, 1f));
            SetMusicVolume(PlayerPrefs.GetFloat(PREF_MUSIC, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(PREF_SFX, 1f));
        }
    }
}