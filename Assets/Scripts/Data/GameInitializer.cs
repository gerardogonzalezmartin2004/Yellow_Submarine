using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    // este script se encarga de inicializar la partida, ya sea creando una nueva o cargando una existente, y de mantener el seguimiento del tiempo de juego durante la sesión actual. También proporciona un método para recopilar los datos necesarios para guardar la partida.
    public class GameInitializer : MonoBehaviour
    {
        [Header("References (arrastra desde la escena)")]
        [SerializeField] private Transform diverTransform;
        [SerializeField] private Transform boatTransform;

      
        private float sessionTime;
        private float loadedPlayTime; 

        public static GameInitializer Instance { get; private set; }

      
        public int ActiveSlot { get; private set; } = -1;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (SceneLoader.Instance == null) return;

            ActiveSlot = SceneLoader.Instance.PendingSlot;

            if (SceneLoader.Instance.IsNewGame)
            {
                Debug.Log($"[GameInitializer] Nueva partida → slot {ActiveSlot}");
                InitNewGame();
            }
            else
            {
                Debug.Log($"[GameInitializer] Cargando partida → slot {ActiveSlot}");
                LoadGame(ActiveSlot);
            }
        }

        private void Update()
        {
          
            if (Time.timeScale > 0f)
                sessionTime += Time.unscaledDeltaTime;
        }

       
        public float GetTotalPlayTime() => loadedPlayTime + sessionTime;

       
        private void InitNewGame()
        {
            sessionTime = 0f;
            loadedPlayTime = 0f;
        }

        private void LoadGame(int slot)
        {
            SaveData data = SaveManager.Instance.Load(slot);
            if (data == null)
            {
                Debug.LogWarning($"[GameInitializer] No existe save en slot {slot}. Iniciando nueva partida.");
                InitNewGame();
                return;
            }

            loadedPlayTime = data.totalPlayTime;
            sessionTime = 0f;

          
            if (diverTransform != null && data.diverPosition != null)
                diverTransform.position = data.diverPosition.ToVector3();

            if (boatTransform != null && data.boatPosition != null)
                boatTransform.position = data.boatPosition.ToVector3();

         

            Debug.Log($"[GameInitializer] Partida cargada: slot {slot}, oro {data.gold}, tiempo {data.totalPlayTime}s");
        }

        public SaveData CollectSaveData()
        {
            var data = new SaveData
            {
                totalPlayTime = GetTotalPlayTime(),
                
            };

            
            if (diverTransform != null)
                data.diverPosition = SerializableVector3.From(diverTransform.position);
            if (boatTransform != null)
                data.boatPosition = SerializableVector3.From(boatTransform.position);

           

            return data;
        }
    }
}