using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace AbyssalReach.UI
{
    //Gestionar los indicadores visuales del modo Buceador
    public class DiverHUDController : MonoBehaviour
    {
        [Header("Referencias de la escena")]
        [Tooltip("Transform del buceador para calcular la profundidad")]
        [SerializeField] private Transform diverTransform;

        [Tooltip("Y del nivel del agua (igual que en DiverMovement.waterSurfaceY)")]
        [SerializeField] private float waterSurfaceY = 0f;

        [Header("UI: Oxígeno")]
        [Tooltip("Image en modo Filled que representa el nivel de oxígeno")]
        [SerializeField] private Image oxygenFillImage;

        [Tooltip("Texto que muestra el % de oxígeno")]
        [SerializeField] private TextMeshProUGUI oxygenText;

        [Header("UI: Salud")]
        [Tooltip("Image en modo Filled que representa la salud del buceador")]
        [SerializeField] private Image healthFillImage;

        [Tooltip("Texto que muestra los puntos de vida")]
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("UI: Profundidad")]
        [Tooltip("Image vertical (Filled, origen abajo) para la profundidad")]
        [SerializeField] private Image depthFillImage;

        [Tooltip("Texto que muestra la profundidad en metros")]
        [SerializeField] private TextMeshProUGUI depthText;

        [Tooltip("Profundidad máxima del juego (para normalizar el fill)")]
        [SerializeField] private float maxDepth = 50f;

        [Header("Colores de Oxígeno")]
        [SerializeField] private Color oxygenSafe = new Color(0.18f, 0.80f, 0.44f);
        [SerializeField] private Color oxygenWarning = new Color(0.90f, 0.49f, 0.13f);
        [SerializeField] private Color oxygenDanger = new Color(0.75f, 0.22f, 0.17f);

        [Header("Colores de Salud")]
        [SerializeField] private Color healthHigh = new Color(0.75f, 0.22f, 0.17f);
        [SerializeField] private Color healthLow = new Color(0.40f, 0.05f, 0.05f);

        [Header("Valores actuales")]
        [Tooltip("% de oxígeno (0-100). Llamar a SetOxygen() desde el sistema de O2 cuando esté listo")]
        [Range(0f, 100f)]
        [SerializeField] private float currentOxygen = 100f;

        [Tooltip("Puntos de vida (0-100)")]
        [Range(0f, 100f)]
        [SerializeField] private float currentHealth = 100f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private float currentDepth = 0f;

        #region Unity ciclo de vida

        private void Start()
        {
            // Buscar el buceador automáticamente si no está asignado
            if (diverTransform == null)
            {
                GameObject diver = GameObject.FindGameObjectWithTag("Diver");

                if (diver != null)
                {
                    diverTransform = diver.transform;

                    if (showDebug)
                    {
                        Debug.Log("[DiverHUD] Buceador encontrado automáticamente");
                    }
                }
                else
                {
                    Debug.LogWarning("[DiverHUD] No se encontró un objeto con tag 'Diver'");
                }
            }
        }

        private void Update()
        {
            UpdateOxygenUI(currentOxygen);
            UpdateHealthUI(currentHealth);
            UpdateDepthUI();
        }

        #endregion

        #region Actualizaciones de UI

        private void UpdateOxygenUI(float oxyPct)
        {
            float normalized = Mathf.Clamp01(oxyPct / 100f);

            if (oxygenFillImage != null)
            {
                oxygenFillImage.fillAmount = normalized;
                oxygenFillImage.color = GetOxygenColor(normalized);
            }

            if (oxygenText != null)
            {
                oxygenText.text = Mathf.RoundToInt(oxyPct) + "%";
            }
        }

        private void UpdateHealthUI(float hp)
        {
            float normalized = Mathf.Clamp01(hp / 100f);

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = normalized;
                healthFillImage.color = Color.Lerp(healthLow, healthHigh, normalized);
            }

            if (healthText != null)
            {
                healthText.text = Mathf.RoundToInt(hp) + "HP";
            }
        }

        private void UpdateDepthUI()
        {
            if (diverTransform == null)
            {
                return;
            }

            // Profundidad = distancia hacia abajo desde la superficie
            currentDepth = Mathf.Max(0f, waterSurfaceY - diverTransform.position.y);
            float normalized = Mathf.Clamp01(currentDepth / maxDepth);

            if (depthFillImage != null)
            {
                depthFillImage.fillAmount = normalized;
            }

            if (depthText != null)
            {
                depthText.text = Mathf.RoundToInt(currentDepth) + "m";
            }
        }

        private Color GetOxygenColor(float normalized)
        {
            if (normalized > 0.5f)
            {
                return Color.Lerp(oxygenWarning, oxygenSafe, (normalized - 0.5f) * 2f);
            }
            else if (normalized > 0.25f)
            {
                return Color.Lerp(oxygenDanger, oxygenWarning, (normalized - 0.25f) * 4f);
            }
            else
            {
                return oxygenDanger;
            }
        }

        #endregion

        #region API Pública

        // Conectar con el sistema de oxígeno cuando esté implementado
        public void SetOxygen(float pct)
        {
            currentOxygen = Mathf.Clamp(pct, 0f, 100f);
        }

        public void SetHealth(float hp)
        {
            currentHealth = Mathf.Clamp(hp, 0f, 100f);
        }

        public float GetCurrentDepth()
        {
            return currentDepth;
        }

        public float GetCurrentOxygen()
        {
            return currentOxygen;
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
            style.normal.textColor = new Color(0.18f, 0.80f, 0.44f);

            GUI.Label(new Rect(10, 410, 300, 20), "[DiverHUD] O2: " + currentOxygen.ToString("F1") + "%", style);
            GUI.Label(new Rect(10, 430, 300, 20), "[DiverHUD] Profund: " + currentDepth.ToString("F1") + "m", style);
            GUI.Label(new Rect(10, 450, 300, 20), "[DiverHUD] HP: " + currentHealth.ToString("F0"), style);
        }

        #endregion
    }
}