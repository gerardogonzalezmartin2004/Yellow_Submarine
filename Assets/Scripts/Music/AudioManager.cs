using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AbyssalReach.Core
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;

        public bool loop = false;
    }

    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance;

        [Header("Mixer")]
        [SerializeField] private AudioMixer masterMixer;

        [Header("Mixer Snapshots")]
        [SerializeField] private AudioMixerSnapshot normalSnapshot;
        [SerializeField] private AudioMixerSnapshot underwaterSnapshot;

        [Header("Sounds")]
        [SerializeField] private List<Sound> sounds = new();

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSourcePrefab;

        private Dictionary<string, Sound> soundDictionary = new();

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        private const string PREF_MASTER = "vol_master";
        private const string PREF_MUSIC = "vol_music";
        private const string PREF_SFX = "vol_sfx";

        private bool underwaterEnabled = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            BuildDictionary();
        }

        private void Start()
        {
            LoadAndApplySaved();
        }

        // =========================
        // VOLUMEN
        // =========================

        public void SetMasterVolume(float v)
        {
            ApplyToMixer(PARAM_MASTER, v);
            PlayerPrefs.SetFloat(PREF_MASTER, v);
        }

        public void SetMusicVolume(float v)
        {
            ApplyToMixer(PARAM_MUSIC, v);
            PlayerPrefs.SetFloat(PREF_MUSIC, v);
        }

        public void SetSFXVolume(float v)
        {
            ApplyToMixer(PARAM_SFX, v);
            PlayerPrefs.SetFloat(PREF_SFX, v);
        }

        public float GetMasterVolume() => PlayerPrefs.GetFloat(PREF_MASTER, 1f);
        public float GetMusicVolume() => PlayerPrefs.GetFloat(PREF_MUSIC, 1f);
        public float GetSFXVolume() => PlayerPrefs.GetFloat(PREF_SFX, 1f);

        private void ApplyToMixer(string param, float linear)
        {
            float dB = linear > 0.0001f
                ? Mathf.Log10(linear) * 20f
                : -80f;

            masterMixer.SetFloat(param, dB);
        }

        private void LoadAndApplySaved()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(PREF_MASTER, 1f));
            SetMusicVolume(PlayerPrefs.GetFloat(PREF_MUSIC, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(PREF_SFX, 1f));
        }

        // =========================
        // SONIDOS
        // =========================

        private void BuildDictionary()
        {
            soundDictionary.Clear();

            foreach (Sound sound in sounds)
            {
                if (!soundDictionary.ContainsKey(sound.name))
                {
                    soundDictionary.Add(sound.name, sound);
                }
            }
        }

        public void PlaySFX(string soundName)
        {
            if (!soundDictionary.ContainsKey(soundName))
            {
                Debug.LogWarning($"No existe el sonido: {soundName}");
                return;
            }

            Sound sound = soundDictionary[soundName];

            AudioSource source = Instantiate(
                sfxSourcePrefab,
                transform.position,
                Quaternion.identity
            );

            source.clip = sound.clip;
            source.volume = sound.volume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;

            source.Play();

            if (!sound.loop)
            {
                Destroy(source.gameObject, sound.clip.length);
            }
        }

        public void PlayMusic(string soundName)
        {
            if (!soundDictionary.ContainsKey(soundName))
            {
                Debug.LogWarning($"No existe la música: {soundName}");
                return;
            }

            Sound sound = soundDictionary[soundName];

            musicSource.clip = sound.clip;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.loop = true;

            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        // =========================
        // UNDERWATER FILTER
        // =========================

        public void SetUnderwater(bool enabled)
        {
            underwaterEnabled = enabled;

            if (underwaterEnabled)
            {
                underwaterSnapshot.TransitionTo(0.5f);
            }
            else
            {
                normalSnapshot.TransitionTo(0.5f);
            }
        }

        public bool IsUnderwater()
        {
            return underwaterEnabled;
        }
    }
}