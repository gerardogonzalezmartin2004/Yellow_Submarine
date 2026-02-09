using UnityEngine;

namespace AbyssalReach.Core
{
    // Este script gestiona el dinero (Oro) del jugador.
    // Es un Singleton, lo que significa que solo hay uno en todo el juego y es accesible desde cualquier lado.
    public class CurrencyManager : MonoBehaviour
    {
        // Instancia estática para acceder desde otros scripts (ej: CurrencyManager.Instance.AddGold(10))
        private static CurrencyManager instance;

        [Header("Currency")]
        [Tooltip("Cantidad actual de oro del jugador")]
        [SerializeField] private int currentGold = 0;

        [Header("Debug")]
        [Tooltip("Si es true, muestra mensajes en consola y en pantalla")]
        [SerializeField] private bool showDebug = true;

        #region Events
              
        public delegate void GoldChanged(int newAmount, int delta);// Definimos un "delegate" q es un tipo de función para el evento. Y funciona como una plantilla para las funciones que se suscriban a este evento. En este caso, cualquier función que quiera escuchar el evento de cambio de oro debe tener esta firma: recibir un int con la nueva cantidad de oro y un int con el cambio (positivo o negativo).

        // Evento al que otros scripts (como la UI) pueden suscribirse para saber cuándo cambia el oro
        public static event GoldChanged OnGoldChanged;

        #endregion

        #region Singleton

        // Propiedad pública para acceder a la instancia
        public static CurrencyManager Instance
        {
            get
            {
                return instance;
            }
        }

        private void Awake()
        {
            // Patrón Singleton: Asegura que solo exista una instancia
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            // DontDestroyOnLoad hace que este objeto no se borre al cambiar de escena
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Aplicaciones

        // Añade una cantidad de oro al total
        public void AddGold(int amount)
        {
            // No permitimos añadir cantidades negativas o cero
            if (amount <= 0)
            {
                return;
            }

            currentGold += amount;

            if (showDebug)
            {
                Debug.Log("[Currency] +" + amount + "G -> Total: " + currentGold + "G");
            }

            // Avisamos a todos los suscritos como la UI que el oro ha cambiado
            if (OnGoldChanged != null)
            {
                OnGoldChanged.Invoke(currentGold, amount);
            }
        }

        // Intenta gastar oro. Devuelve true si se pudo gastar, false si no había suficiente.
        public bool SpendGold(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            // Verificamos si tenemos suficiente dinero
            if (currentGold >= amount)
            {
                currentGold -= amount;

                if (showDebug)
                {
                    Debug.Log("[Currency] -" + amount + "G -> Total: " + currentGold + "G");
                }

                // Notificamos el cambio (el delta es negativo porque restamos)
                if (OnGoldChanged != null)
                {
                    OnGoldChanged.Invoke(currentGold, -amount);
                }

                return true; // Compra exitosa
            }

            // Si llegamos aquí, es que no había suficiente oro
            if (showDebug)
            {
                Debug.LogWarning("[Currency] Not enough gold! Need: " + amount + "G, Have: " + currentGold + "G");
            }

            return false; // Compra fallida
        }

        // Verifica si hay suficiente oro sin gastarlo 
        public bool HasEnoughGold(int amount)
        {
            if (currentGold >= amount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // Obtiene la cantidad actual de oro
        public int GetGold()
        {
            return currentGold;
        }

        // Establece el oro directamente 
        public void SetGold(int amount)
        {
            int delta = amount - currentGold;
            currentGold = amount;

            if (OnGoldChanged != null)
            {
                OnGoldChanged.Invoke(currentGold, delta);
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnGUI()
        {            
            if (!showDebug)
            {
                return;
            }

            // Configuración del estilo del texto en pantalla
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            // Dibujamos la etiqueta en la esquina superior derecha
            // Rect(x, y, ancho, alto) como en otros scripts
            GUI.Label(new Rect(Screen.width - 150, 10, 140, 25), "Gold: " + currentGold + "G", style);
        }

        #endregion
    }
}