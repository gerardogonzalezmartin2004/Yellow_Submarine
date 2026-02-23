using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Gameplay;

namespace AbyssalReach.UI
{
    // Controla el indicador visual de tensión del cable (Tether).
    // Lee TetherSystem.GetTension() (0 a 1) y actualiza:
    //   - Image.fillAmount del arco semicircular
    //   - Color del arco (verde → naranja → rojo)
    //   - Texto de porcentaje
    //   - Vibración del panel cuando llega al límite
    //   - Texto de estado ("LIBRE", "TENSO", "MÁXIMO")
    public class TetherHUDController : MonoBehaviour
    {
        [Header("Referencias al TetherSystem")]
        [Tooltip("Arrastra aquí el GameObject que tiene el TetherSystem")]
        [SerializeField] private TetherSystem tetherSystem;

        [Header("UI del Arco de Tensión")]
        [Tooltip("Image en modo Filled/Radial que representa la tensión. fillAmount = tension")]
        [SerializeField] private Image tensionArcImage;

        [Tooltip("Texto que muestra el porcentaje (ej: '78%')")]
        [SerializeField] private TextMeshProUGUI tensionPercentText;

        [Tooltip("Texto de estado ('CABLE LIBRE', 'TENSO', 'LÍMITE')")]
        [SerializeField] private TextMeshProUGUI tensionStatusText;

        [Header("Colores según tensión")]
        [Tooltip("Color cuando el cable está relajado (tension < tensionWarning)")]
        [SerializeField] private Color colorFree = new Color(0.18f, 0.80f, 0.44f); // Verde

        [Tooltip("Color de advertencia (tension entre warning y danger)")]
        [SerializeField] private Color colorWarning = new Color(0.90f, 0.49f, 0.13f); // Naranja

        [Tooltip("Color de peligro máximo (tension > tensionDanger)")]
        [SerializeField] private Color colorDanger = new Color(0.75f, 0.22f, 0.17f); // Rojo

        [Header("Umbrales")]
        [Tooltip("A partir de este valor (0-1) la UI se pone en modo Advertencia")]
        [SerializeField] private float tensionWarning = 0.5f;

        [Tooltip("A partir de este valor (0-1) la UI se pone en modo Peligro")]
        [SerializeField] private float tensionDanger = 0.85f;

        [Header("Vibración")]
        [Tooltip("El panel completo que vibrará cuando el cable esté al límite")]
        [SerializeField] private RectTransform shakeTarget;

        [Tooltip("Intensidad de la vibración en píxeles")]
        [SerializeField] private float shakeIntensity = 3f;

        [Tooltip("Velocidad de la vibración")]
        [SerializeField] private float shakeSpeed = 30f;

        [Tooltip("Umbral a partir del cual se activa la vibración")]
        [SerializeField] private float shakeThreshold = 0.85f;

        [Header("Textos de estado")]
        [SerializeField] private string textFree = "CABLE LIBRE";
        [SerializeField] private string textWarning = "CABLE TENSO";
        [SerializeField] private string textDanger = "⚠ LÍMITE MÁXIMO";

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // Posición original del shakeTarget para restaurarla
        private Vector3 shakeOriginPos;
        private bool isShaking = false;
        private float currentTension = 0f;

        #region Unity ciclo de vida

        private void Start()
        {
            // Cachear posición original del panel para vibración
            if (shakeTarget != null)
            {
                shakeOriginPos = shakeTarget.localPosition;
            }

            // Intentar encontrar el TetherSystem automáticamente si no está asignado
            if (tetherSystem == null)
            {
                tetherSystem = FindFirstObjectByType<TetherSystem>();

                if (tetherSystem == null)
                {
                    Debug.LogWarning("[TetherHUD] No se encontró un TetherSystem en la escena");
                }
                else if (showDebug)
                {
                    Debug.Log("[TetherHUD] TetherSystem encontrado automáticamente");
                }
            }

            // Estado inicial: cable libre al 0%
            UpdateVisuals(0f);
        }

        private void Update()
        {
            if (tetherSystem == null)
            {
                return;
            }

            // Leer la tensión directamente del TetherSystem
            currentTension = tetherSystem.GetTension();

            UpdateVisuals(currentTension);
            UpdateShake(currentTension);
        }

        #endregion

        #region Update Visual

        private void UpdateVisuals(float tension)
        {
            // 1) Actualizar el fillAmount del arco
            if (tensionArcImage != null)
            {
                tensionArcImage.fillAmount = tension;
            }

            // 2) Calcular y aplicar el color según umbrales
            Color targetColor = CalculateColor(tension);

            if (tensionArcImage != null)
            {
                tensionArcImage.color = targetColor;
            }

            // 3) Texto de porcentaje
            if (tensionPercentText != null)
            {
                int pct = Mathf.RoundToInt(tension * 100f);
                tensionPercentText.text = pct + "%";
                tensionPercentText.color = targetColor;
            }

            // 4) Texto de estado
            if (tensionStatusText != null)
            {
                if (tension >= tensionDanger)
                {
                    tensionStatusText.text = textDanger;
                    tensionStatusText.color = colorDanger;
                }
                else if (tension >= tensionWarning)
                {
                    tensionStatusText.text = textWarning;
                    tensionStatusText.color = colorWarning;
                }
                else
                {
                    tensionStatusText.text = textFree;
                    tensionStatusText.color = colorFree;
                }
            }
        }

        private Color CalculateColor(float tension)
        {
            // Interpolación en dos tramos:
            // [0 → warning]:           verde → naranja
            // [warning → 1]:           naranja → rojo

            if (tension < tensionWarning)
            {
                float t = tension / tensionWarning; // Normalizar al rango [0, 1]
                return Color.Lerp(colorFree, colorWarning, t);
            }
            else
            {
                float t = (tension - tensionWarning) / (1f - tensionWarning);
                return Color.Lerp(colorWarning, colorDanger, t);
            }
        }

        #endregion

        #region Shake Logic

        private void UpdateShake(float tension)
        {
            if (shakeTarget == null)
            {
                return;
            }

            // Activar vibración cuando supera el umbral
            if (tension >= shakeThreshold)
            {
                isShaking = true;

                // La intensidad de vibración escala con la tensión extra
                float extraTension = (tension - shakeThreshold) / (1f - shakeThreshold);
                float currentIntensity = shakeIntensity * extraTension;

                // Vibración usando Mathf.Sin para movimiento oscilante suave
                float offsetX = Mathf.Sin(Time.time * shakeSpeed) * currentIntensity;
                float offsetY = Mathf.Sin(Time.time * shakeSpeed * 1.3f) * currentIntensity * 0.5f;

                shakeTarget.localPosition = shakeOriginPos + new Vector3(offsetX, offsetY, 0f);
            }
            else if (isShaking)
            {
                // Restaurar posición original cuando cesa la tensión
                isShaking = false;
                shakeTarget.localPosition = shakeOriginPos;
            }
        }

        #endregion

        #region API Pública

        // Permite asignar el TetherSystem en tiempo de ejecución
        // (útil cuando el TetherSystem se activa al entrar en modo Buceo)
        public void SetTetherSystem(TetherSystem tether)
        {
            tetherSystem = tether;

            if (showDebug)
            {
                Debug.Log("[TetherHUD] TetherSystem asignado en runtime");
            }
        }

        public float GetCurrentTension()
        {
            return currentTension;
        }

        #endregion

        #region Debug (GUI)

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 12;
            style.normal.textColor = Color.cyan;

            GUI.Label(new Rect(10, 370, 300, 20), "[TetherHUD] Tensión: " + currentTension.ToString("F3"), style);
            GUI.Label(new Rect(10, 390, 300, 20), "[TetherHUD] Shaking: " + isShaking, style);
        }

        #endregion
    }
}