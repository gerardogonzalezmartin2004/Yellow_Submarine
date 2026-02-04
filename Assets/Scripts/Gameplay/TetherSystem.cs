using UnityEngine;

namespace AbyssalReach.Gameplay
{
     [RequireComponent(typeof(LineRenderer))] // No es esta mal tenerlo, por si acaso.
    public class TetherSystem : MonoBehaviour
    {
        // En este script hya un sistema de cable que conecta el barco con el buceador. Y limita la distancia máxima entre ambos.
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

       
        private LineRenderer lineRenderer;
        private Rigidbody diverRb;

       
        private float currentLength = 0f;
        private float tension = 0f;

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
            // Ya que estamos por si se desconfigura. Luego se me va.
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;            
            lineRenderer.startColor = relaxedColor;
            lineRenderer.endColor = relaxedColor;
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;
        }

        private void ValidateReferences()
        {
            if (boatAnchor == null)
            {
                Debug.LogError("[TetherSystem] Boat Anchor no esta asignado");
            }

            if (diverAnchor == null)
            {
                Debug.LogError("[TetherSystem] Diver Anchor no esta asignado");
            }

            if (diverRb == null && diverAnchor != null)
            {
                Debug.LogWarning("[TetherSystem] Diver no tiene Rigidbody");
            }
        }

        #endregion

        #region Tether Fisicas

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
                  Debug.Log("[Tether] Length: " + currentLength+ ":F2}/{" + maxLength+ ":F2} | Tension: {"+ tension+ ":F2}");
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

        
        
      // Verifica si el cable está al límite
        public bool IsAtMaxLength => currentLength >= maxLength * 0.99f;

       
        
        // Obtiene la longitud actual del cable
        public float CurrentLength => currentLength;

        
       
        // Obtiene la longitud máxima
        public float MaxLength => maxLength;

       // Obtiene la tensión actual (0-1)
        public float Tension => tension;

       
        // Mejora la longitud del cable (para upgrades futuros)
        
        public void UpgradeLength(float newLength)
        {
            if (newLength > maxLength)
            {
                maxLength = newLength;
                Debug.Log("[TetherSystem] Cable upgraded to {"+newLength+"}m");
               
            }
        }

     
        
        // Asigna las referencias 
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