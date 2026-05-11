using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Colocado en la escena de juego (GameScene).
    /// En Start() detecta si es partida nueva o cargada y aplica los datos.
    /// También lleva el tiempo de partida actual para guardarlo después.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        [Header("References (arrastra desde la escena)")]
        [SerializeField] private Transform diverTransform;
        [SerializeField] private Transform boatTransform;

        // Tiempo de juego de la sesión actual
        private float sessionTime;
        private float loadedPlayTime; // tiempo acumulado del save

        public static GameInitializer Instance { get; private set; }

        // El slot activo en esta sesión
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
            // Acumula tiempo solo cuando el juego no está pausado
            if (Time.timeScale > 0f)
                sessionTime += Time.unscaledDeltaTime;
        }

        // ─── Tiempo total para guardar ────────────────────────────────────────
        public float GetTotalPlayTime() => loadedPlayTime + sessionTime;

        // ─── Nueva partida ────────────────────────────────────────────────────
        private void InitNewGame()
        {
            sessionTime = 0f;
            loadedPlayTime = 0f;
            // Aquí puedes poner al barco y buzo en sus posiciones de inicio por defecto
        }

        // ─── Cargar partida guardada ──────────────────────────────────────────
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

            // ── Posiciones ────────────────────────────────────────────────────
            if (diverTransform != null && data.diverPosition != null)
                diverTransform.position = data.diverPosition.ToVector3();

            if (boatTransform != null && data.boatPosition != null)
                boatTransform.position = data.boatPosition.ToVector3();

            // ── Economía ──────────────────────────────────────────────────────
            // TODO: YourEconomyManager.Instance.SetGold(data.gold);

            // ── Mejoras de tienda ─────────────────────────────────────────────
            // TODO: ShopManager.Instance.ApplyUpgrades(data.purchasedUpgrades);

            // ── Props del entorno ─────────────────────────────────────────────
            // TODO: PropManager.Instance.MarkCollected(data.collectedPropIds);

            // ── Inventarios ───────────────────────────────────────────────────
            // TODO: InventoryController.Instance.LoadBoatInventory(data.boatInventory);
            // TODO: InventoryController.Instance.LoadDiverInventory(data.diverInventory);

            Debug.Log($"[GameInitializer] Partida cargada: slot {slot}, oro {data.gold}, tiempo {data.totalPlayTime}s");
        }

        // ─── Recolectar datos para guardar (llamado por PauseMenuController) ──
        public SaveData CollectSaveData()
        {
            var data = new SaveData
            {
                totalPlayTime = GetTotalPlayTime(),
                // TODO: gold = YourEconomyManager.Instance.GetGold(),
                // TODO: score = ...
                // TODO: currentLevel = ...
            };

            // Posiciones
            if (diverTransform != null)
                data.diverPosition = SerializableVector3.From(diverTransform.position);
            if (boatTransform != null)
                data.boatPosition = SerializableVector3.From(boatTransform.position);

            // TODO: data.purchasedUpgrades = ShopManager.Instance.GetPurchasedIds();
            // TODO: data.collectedPropIds   = PropManager.Instance.GetCollectedIds();
            // TODO: data.boatInventory      = InventoryController.Instance.GetBoatItems();
            // TODO: data.diverInventory     = InventoryController.Instance.GetDiverItems();

            return data;
        }
    }
}