using UnityEngine;

namespace AbyssalReach.Gameplay
{
    /// <summary>
    /// Sistema de cable que conecta el barco con el buceador.
    /// Limita la distancia y aplica física de "compás" (arco de 180°).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TetherSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform del barco (punto de anclaje superior)")]
        [SerializeField] private Transform boatAnchor;

        [Tooltip("Transform del buceador (punto de anclaje inferior)")]
        [SerializeField] private Transform diverAnchor;

        [Header("Tether Properties")]
        [Tooltip("Longitud máxima del cable en metros")]
        [SerializeField] private float maxLength = 30f;

        [Tooltip("Fuerza aplicada cuando se excede la longitud")]
        [SerializeField] private float pullForce = 50f;

        [Tooltip("A partir de qué porcentaje empieza la tensión (0-1)")]
        [SerializeField] private float tensionThreshold = 0.9f;

        [Header("Visual")]
        [Tooltip("Ancho del cable")]
        [SerializeField] private float lineWidth = 0.1f;

        [Tooltip("Color cuando está relajado")]
        [SerializeField] private Color relaxedColor = Color.gray;

        [Tooltip("Color cuando está tenso")]
        [SerializeField] private Color tenseColor = Color.red;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // Components
        private LineRenderer lineRenderer;
        private Rigidbody diverRb;

        // State
        private float currentLength = 0f;
        private float tension = 0f; // 0-1

        #region Unity Lifecycle

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            SetupLineRenderer();
        }

        private void Start()
        {
            if (diverAnchor != null)
            {
                diverRb = diverAnchor.GetComponent<Rigidbody>();
            }

            ValidateReferences();
        }

        private void LateUpdate()
        {
            UpdateTetherVisual();
        }

        private void FixedUpdate()
        {
            UpdateTetherPhysics();
        }

        #endregion

        #region Setup

        private void SetupLineRenderer()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = relaxedColor;
            lineRenderer.endColor = relaxedColor;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;
        }

        private void ValidateReferences()
        {
            if (boatAnchor == null)
            {
                Debug.LogError("[TetherSystem] Boat Anchor not assigned!");
            }

            if (diverAnchor == null)
            {
                Debug.LogError("[TetherSystem] Diver Anchor not assigned!");
            }

            if (diverRb == null && diverAnchor != null)
            {
                Debug.LogWarning("[TetherSystem] Diver doesn't have Rigidbody. Pull force won't work.");
            }
        }

        #endregion

        #region Tether Physics

        private void UpdateTetherPhysics()
        {
            if (boatAnchor == null || diverAnchor == null) return;

            // Calcular longitud actual
            currentLength = Vector3.Distance(boatAnchor.position, diverAnchor.position);

            // Calcular tensión (0 = relajado, 1 = máximo)
            tension = Mathf.Clamp01((currentLength - (maxLength * tensionThreshold)) / (maxLength * (1f - tensionThreshold)));

            // Si excede la longitud máxima, aplicar fuerza de retroceso
            if (currentLength > maxLength)
            {
                ApplyPullForce();
            }

            if (showDebug)
            {
                Debug.Log($"[Tether] Length: {currentLength:F2}/{maxLength:F2} | Tension: {tension:F2}");
            }
        }

        private void ApplyPullForce()
        {
            if (diverRb == null) return;

            // Dirección desde buceador hacia barco
            Vector3 direction = (boatAnchor.position - diverAnchor.position).normalized;

            // Fuerza proporcional a cuánto excede el límite
            float excessLength = currentLength - maxLength;
            float forceMagnitude = pullForce * excessLength;

            // Aplicar fuerza
            diverRb.AddForce(direction * forceMagnitude, ForceMode.Force);

            if (showDebug)
            {
                Debug.DrawRay(diverAnchor.position, direction * 2f, Color.red);
            }
        }

        #endregion

        #region Visual

        private void UpdateTetherVisual()
        {
            if (boatAnchor == null || diverAnchor == null) return;

            // Actualizar posiciones del LineRenderer
            lineRenderer.SetPosition(0, boatAnchor.position);
            lineRenderer.SetPosition(1, diverAnchor.position);

            // Cambiar color según tensión
            Color currentColor = Color.Lerp(relaxedColor, tenseColor, tension);
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Verifica si el cable está al límite
        /// </summary>
        public bool IsAtMaxLength => currentLength >= maxLength * 0.99f;

        /// <summary>
        /// Obtiene la longitud actual del cable
        /// </summary>
        public float CurrentLength => currentLength;

        /// <summary>
        /// Obtiene la longitud máxima
        /// </summary>
        public float MaxLength => maxLength;

        /// <summary>
        /// Obtiene la tensión actual (0-1)
        /// </summary>
        public float Tension => tension;

        /// <summary>
        /// Mejora la longitud del cable (para upgrades futuros)
        /// </summary>
        public void UpgradeLength(float newLength)
        {
            if (newLength > maxLength)
            {
                maxLength = newLength;
                Debug.Log($"[TetherSystem] Cable upgraded to {newLength}m");
            }
        }

        /// <summary>
        /// Asigna las referencias dinámicamente
        /// </summary>
        public void SetAnchors(Transform boat, Transform diver)
        {
            boatAnchor = boat;
            diverAnchor = diver;

            if (diver != null)
            {
                diverRb = diver.GetComponent<Rigidbody>();
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // Dibujar radio máximo del cable
            if (boatAnchor != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                DrawCircle(boatAnchor.position, maxLength, 30);
            }

            // Dibujar la línea del cable
            if (boatAnchor != null && diverAnchor != null)
            {
                Gizmos.color = IsAtMaxLength ? Color.red : Color.yellow;
                Gizmos.DrawLine(boatAnchor.position, diverAnchor.position);
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        #endregion
    }
}