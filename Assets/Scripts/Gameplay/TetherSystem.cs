using UnityEngine;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(LineRenderer))]
    public class TetherSystem : MonoBehaviour
    {
        // En este script hay un sistema de cable que conecta el barco con el buceador.
        // Limita la distancia máxima entre ambos.

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
            // Configuración básica de la línea visual
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
            if (boatAnchor == null || diverAnchor == null)
            {
                return;
            }

            // Calcular distancia real
            currentLength = Vector3.Distance(boatAnchor.position, diverAnchor.position);

            //  Calcular factor de tensión
            // Esto convierte la distancia en un valor de 0 a 1 basándose en el umbral
            float range = maxLength * (1f - tensionThreshold);
            float excessOverThreshold = currentLength - (maxLength * tensionThreshold);

            tension = Mathf.Clamp01(excessOverThreshold / range);

            //  Aplicar físicas si nos pasamos
            if (currentLength > maxLength)
            {
                ApplyPullForce();
            }

            Debug.Log("[Tether] Length: " + currentLength + "/" + maxLength + " | Tension: " + tension);
        }

        private void ApplyPullForce()
        {
            if (diverRb == null)
            {
                return;
            }

            // Calcular dirección: Desde el buzo hacia el barco.
            Vector3 direction = (boatAnchor.position - diverAnchor.position).normalized; // Es como si fuese una flecha invisible que apunta desde el buzo hacia el barco. Al normalizarlo, la flecha mide exactamente 1 metro, lo que nos permite multiplicarla después por la fuerza que queramos.

            // Calcular fuerza. Cuanto más lejos, más fuerte tira como si fuese un muelle
            float excessLength = currentLength - maxLength;
            float forceMagnitude = pullForce * excessLength;

            // Aplicar la fuerza al Rigidbody
            diverRb.AddForce(direction * forceMagnitude, ForceMode.Force);

            // Debug visual del vector de fuerza
            Debug.DrawRay(diverAnchor.position, direction * 2f, Color.red);
        }

        #endregion

        #region Visual

        private void UpdateTetherVisual()
        {
            if (boatAnchor == null || diverAnchor == null)
            {
                return;
            }

            // Actualizar los puntos de la línea
            lineRenderer.SetPosition(0, boatAnchor.position);
            lineRenderer.SetPosition(1, diverAnchor.position);

            // Interpolar color entre gris (relajado) y rojo (tenso) según la tensión
            Color currentColor = Color.Lerp(relaxedColor, tenseColor, tension);
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }

        #endregion

        #region Public API


        // Sirve como para que otros scripts puedan consultar el estado del cable, concretamente si este esta estirado al max.
        public bool IsAtMaxLength()
        {
            return currentLength >= maxLength * 0.99f;
        }

        public float GetCurrentLength()
        {
            return currentLength;
        }

        public float GetMaxLength()
        {
            return maxLength;
        }

        public float GetTension()
        {
            return tension;
        }

        public void UpgradeLength(float newLength)
        {
            if (newLength > maxLength)
            {
                maxLength = newLength;
                Debug.Log("[TetherSystem] Cable upgraded to " + newLength + "m");
            }
        }

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

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
           

            if (boatAnchor != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                DrawCircle(boatAnchor.position, maxLength, 30);
            }

            if (boatAnchor != null && diverAnchor != null)
            {
                if (IsAtMaxLength())
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }
                Gizmos.DrawLine(boatAnchor.position, diverAnchor.position);
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments) // este metodo es para dibujar un circulo alrededor del barco que representa la longitud máxima del cable. Y la diferencia es q es 3D, no 2D, por eso el punto del buzo no esta exactamente en el centro del circulo sino q esta a una altura determinada. 
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3( Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        #endregion
    }
}