using UnityEngine;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Singleton que gestiona el dinero (Gold) del jugador.
    /// </summary>
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Header("Currency")]
        [SerializeField] private int currentGold = 0;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        #region Events

        public delegate void GoldChanged(int newAmount, int delta);
        public static event GoldChanged OnGoldChanged;

        #endregion

        #region Singleton

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Añade oro
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;

            currentGold += amount;

            if (showDebug)
            {
                Debug.Log($"[Currency] +{amount}G → Total: {currentGold}G");
            }

            OnGoldChanged?.Invoke(currentGold, amount);
        }

        /// <summary>
        /// Gasta oro si hay suficiente
        /// </summary>
        /// <returns>True si se pudo gastar, False si no hay suficiente</returns>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return false;

            if (currentGold >= amount)
            {
                currentGold -= amount;

                if (showDebug)
                {
                    Debug.Log($"[Currency] -{amount}G → Total: {currentGold}G");
                }

                OnGoldChanged?.Invoke(currentGold, -amount);
                return true;
            }

            if (showDebug)
            {
                Debug.LogWarning($"[Currency] Not enough gold! Need: {amount}G, Have: {currentGold}G");
            }

            return false;
        }

        /// <summary>
        /// Verifica si hay suficiente oro
        /// </summary>
        public bool HasEnoughGold(int amount)
        {
            return currentGold >= amount;
        }

        /// <summary>
        /// Obtiene la cantidad actual de oro
        /// </summary>
        public int GetGold()
        {
            return currentGold;
        }

        /// <summary>
        /// Establece directamente la cantidad de oro (para save/load)
        /// </summary>
        public void SetGold(int amount)
        {
            int delta = amount - currentGold;
            currentGold = amount;
            OnGoldChanged?.Invoke(currentGold, delta);
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showDebug) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(Screen.width - 150, 10, 140, 25),
                $"Gold: {currentGold}G", style);
        }

        #endregion
    }
}