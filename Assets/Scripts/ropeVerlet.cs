using AbyssalReach.Core;
using UnityEngine;
using System.Collections.Generic;

namespace AbyssalReach.Gameplay
{
    /// <summary>
    /// Sistema de cuerda realista usando Verlet Integration
    /// Totalmente en 2D, integrado con TetherSystem
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ropeVerlet : MonoBehaviour
    {
        #region Rope Segment Struct

        public struct ropeSegment
        {
            public Vector2 CurrentPosition;
            public Vector2 OldPosition;

            public ropeSegment(Vector2 pos)
            {
                CurrentPosition = pos;
                OldPosition = pos;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Rope Structure")]
        [Tooltip("Número de segmentos de la cuerda")]
        [SerializeField] private int numOfRopeSegments = 50;

        [Tooltip("Longitud de cada segmento (se calcula automáticamente)")]
        [SerializeField] private float ropeSegmentLength = 0.225f;

        [Header("Physics")]
        [Tooltip("Fuerza de gravedad aplicada")]
        [SerializeField] private Vector2 gravityForce = new Vector2(0f, -1f);

        [Tooltip("Factor de amortiguación (0-1)")]
        [SerializeField] private float dampingFactor = 0.98f;

        [Header("Collision")]
        [Tooltip("Layers con las que colisiona la cuerda")]
        [SerializeField] private LayerMask collisionMask;

        [Tooltip("Radio de colisión de cada segmento")]
        [SerializeField] private float collisionRadius = 0.1f;

        [Tooltip("Factor de rebote en colisiones (0-1)")]
        [SerializeField] private float bounceFactor = 0.1f;

        [Tooltip("Intervalo de segmentos para check de colisión (optimización)")]
        [SerializeField] private int collisionSegmentInterval = 2;

        [Header("Constraints")]
        [Tooltip("Número de iteraciones para resolver constraints")]
        [SerializeField] private int numOfConstraintRuns = 80;

        [Header("Tension Force")]
        [Tooltip("Fuerza base de tensión aplicada al buceador")]
        [SerializeField] private float tensionStrength = 25f;

        [Tooltip("Multiplicador de fuerza en ascenso de emergencia")]
        [SerializeField] private float emergencyForceMultiplier = 20f;

        [Tooltip("Threshold de estiramiento para aplicar tensión (0-1)")]
        [SerializeField] private float tensionThreshold = 0.92f;

        [Header("Anchors")]
        [Tooltip("Transform del punto inicial (barco)")]
        [SerializeField] private Transform ropeStartTransform;

        [Tooltip("Transform del punto final (buceador)")]
        [SerializeField] private Transform ropeEndTransform;

        [Tooltip("¿Anclar el inicio al barco?")]
        [SerializeField] private bool anchorStart = true;

        [Tooltip("¿Anclar el final al buceador?")]
        [SerializeField] private bool anchorEnd = true;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;
        [SerializeField] private bool showTensionDirection = true;


        #endregion

        #region Private Fields

        private LineRenderer lineRenderer;
        private List<ropeSegment> ropeSegments = new List<ropeSegment>();
        private GameController gameController;
        private Rigidbody2D diverRb2D;
        private float maxRopeCapacity = 30f; // Lo que compras en la tienda
        private float activeRopeLength = 30f; // Lo que mide físicamente AHORA

        private float maxRopeLength = 30f;
        private Vector2 tensionDirection = Vector2.up;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Obtener referencias
            gameController = GameController.Instance;
            lineRenderer = GetComponent<LineRenderer>();

            if (ropeEndTransform != null)
            {
                diverRb2D = ropeEndTransform.GetComponent<Rigidbody2D>(); // 2D

                if (diverRb2D == null && showDebug)
                {
                    Debug.LogWarning("[RopeVerlet] Diver no tiene Rigidbody2D");
                }
            }

            InitializeRope();
        }

        private void Update()
        {
            DrawRope();
        }

        private void FixedUpdate()
        {
            Simulate();
            ApplyTensionForce();

            for (int i = 0; i < numOfConstraintRuns; i++)
            {
                ApplyConstraints();

                // Optimización: colisiones cada N iteraciones
                if (i % collisionSegmentInterval == 0)
                {
                    HandleCollisions();
                }
            }
        }

        #endregion

        #region Initialization

        private void InitializeRope()
        {
            if (ropeStartTransform == null)
            {
                Debug.LogError("[RopeVerlet] ropeStartTransform no asignado");
                return;
            }
            activeRopeLength = maxRopeLength;
            // Calcular longitud de segmento inicial
            CalculateSegmentLength();

            // Crear segmentos
            Vector2 startPos = ropeStartTransform.position;
            ropeSegments.Clear();

            for (int i = 0; i < numOfRopeSegments; i++)
            {
                ropeSegments.Add(new ropeSegment(startPos));
                startPos.y -= ropeSegmentLength;
            }

            // Configurar LineRenderer
            lineRenderer.positionCount = numOfRopeSegments;

           
        }

        private void CalculateSegmentLength()
        {
            if (numOfRopeSegments > 1)
            {
                ropeSegmentLength = activeRopeLength / (numOfRopeSegments - 1);
            }
        }

        #endregion

        #region Simulation

        private void Simulate()
        {
            for (int i = 0; i < ropeSegments.Count; i++)
            {
                ropeSegment segment = ropeSegments[i];

                // Verlet integration
                Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * dampingFactor;
                segment.OldPosition = segment.CurrentPosition;
                segment.CurrentPosition += velocity;
                segment.CurrentPosition += gravityForce * Time.fixedDeltaTime;

                ropeSegments[i] = segment;
            }
        }

        #endregion

        #region Constraints

        private void ApplyConstraints()
        {
            // Anclar inicio (barco)
            if (anchorStart && ropeStartTransform != null)
            {
                ropeSegment first = ropeSegments[0];
                first.CurrentPosition = ropeStartTransform.position;
                first.OldPosition = ropeStartTransform.position;
                ropeSegments[0] = first;
            }

            // Anclar final (buceador)
            if (anchorEnd && ropeEndTransform != null)
            {
                int lastIndex = ropeSegments.Count - 1;
                ropeSegment last = ropeSegments[lastIndex];
                last.CurrentPosition = ropeEndTransform.position;
                ropeSegments[lastIndex] = last;
            }

            // Constraints de distancia entre segmentos
            for (int i = 0; i < ropeSegments.Count - 1; i++)
            {
                ropeSegment a = ropeSegments[i];
                ropeSegment b = ropeSegments[i + 1];

                Vector2 delta = b.CurrentPosition - a.CurrentPosition;
                float dist = delta.magnitude;

                if (dist < 0.0001f) continue; // Evitar división por cero

                float error = dist - ropeSegmentLength;
                Vector2 correction = delta.normalized * error * 0.5f;

                // Aplicar corrección según la posición en la cuerda
                if (i == 0 && anchorStart)
                {
                    // Primer segmento anclado, solo mover el siguiente
                    b.CurrentPosition -= correction * 2f;
                }
                else if (i == ropeSegments.Count - 2 && anchorEnd)
                {
                    // Último segmento anclado, solo mover el anterior
                    a.CurrentPosition += correction * 2f;
                }
                else
                {
                    // Segmentos intermedios, mover ambos
                    a.CurrentPosition += correction;
                    b.CurrentPosition -= correction;
                }

                ropeSegments[i] = a;
                ropeSegments[i + 1] = b;
            }
        }

        #endregion

        #region Collision

        private void HandleCollisions()
        {
            // Optimización: solo verificar segmentos no anclados
            int startIndex = anchorStart ? 1 : 0;
            int endIndex = anchorEnd ? ropeSegments.Count - 1 : ropeSegments.Count;

            for (int i = startIndex; i < endIndex; i++)
            {
                ropeSegment segment = ropeSegments[i];
                Vector2 velocity = segment.CurrentPosition - segment.OldPosition;

                // Detectar colisiones
                Collider2D[] colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, collisionRadius, collisionMask);

                foreach (Collider2D collider in colliders)
                {
                    Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                    float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                    if (distance < collisionRadius)
                    {
                        // Calcular normal de colisión
                        Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;

                        if (normal == Vector2.zero)
                        {
                            normal = (segment.CurrentPosition - (Vector2)collider.transform.position).normalized;
                        }

                        // Resolver penetración
                        float depth = collisionRadius - distance;
                        segment.CurrentPosition += normal * depth;

                        // Aplicar rebote
                        velocity = Vector2.Reflect(velocity, normal) * bounceFactor;
                    }
                }

                // Actualizar velocidad después de colisiones
                segment.OldPosition = segment.CurrentPosition - velocity;
                ropeSegments[i] = segment;
            }
        }

        #endregion

        #region Tension Force

        private void ApplyTensionForce()
        {
            Rigidbody2D diverRb2D = ropeEndTransform != null ? ropeEndTransform.GetComponent<Rigidbody2D>() : null;
            

            bool isEmergency = gameController != null && gameController.IsEmergencyAscent();

            // Calcular longitud actual de la cuerda
            float currentLength = GetCurrentRopeLength();
            float totalLength = (numOfRopeSegments - 1) * ropeSegmentLength;

            // Solo aplicar tensión si está lo suficientemente estirada o es emergencia
            if (currentLength < totalLength * tensionThreshold && !isEmergency)
            {
                return;
            }

            // Calcular dirección de tensión (siguiendo la cuerda)
            CalculateTensionDirection();

            // Calcular fuerza
            float force = tensionStrength;

            if (isEmergency)
            {
                force *= emergencyForceMultiplier;

                // En emergencia, reducir drag para ascenso rápido
                diverRb2D.linearDamping = 0f;
                diverRb2D.mass = 0.5f;
            }
            else
            {
                // Valores normales
                diverRb2D.linearDamping = 0f;
                diverRb2D.mass = 10f;
            }

            // Aplicar fuerza en dirección de la cuerda (2D)
            diverRb2D.AddForce(tensionDirection * force, ForceMode2D.Force);

           
        }

        private void CalculateTensionDirection()
        {
            if (ropeSegments.Count >= 2)
            {
                int last = ropeSegments.Count - 1;
                int prev = last - 1;

                Vector2 diverPos = ropeSegments[last].CurrentPosition;
                Vector2 prevPos = ropeSegments[prev].CurrentPosition;

                tensionDirection = (prevPos - diverPos).normalized;



                Debug.DrawRay(diverPos, tensionDirection * 6f, Color.red, 0.15f);
            }

            
            
        }

        #endregion

        #region Length Calculation

        private float GetCurrentRopeLength()
        {
            float length = 0f;

            for (int i = 0; i < ropeSegments.Count - 1; i++)
            {
                length += Vector2.Distance(
                    ropeSegments[i].CurrentPosition,
                    ropeSegments[i + 1].CurrentPosition
                );
            }

            return length;
        }
        public void ReelIn(float amount)
        {
            // Nunca dejamos que la cuerda mida menos de 1 metro para no romper el Verlet
            activeRopeLength = Mathf.Max(1f, activeRopeLength - amount);
            CalculateSegmentLength(); // Ajusta los segmentos, tirando del buzo suavemente
        }

        #endregion

        #region Visual

        private void DrawRope()
        {
            if (lineRenderer == null || ropeSegments.Count == 0) return;

            Vector3[] ropePositions = new Vector3[numOfRopeSegments];

            for (int i = 0; i < ropeSegments.Count; i++)
            {
                ropePositions[i] = ropeSegments[i].CurrentPosition;
            }

            lineRenderer.SetPositions(ropePositions);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Establece la longitud máxima de la cuerda
        /// </summary>
        public void SetMaxLength(float length)
        {
            maxRopeLength = length;
            activeRopeLength = length;
            CalculateSegmentLength();

          
        }

        /// <summary>
        /// Obtiene la longitud máxima de la cuerda
        /// </summary>
        public float GetMaxLength()
        {
            return maxRopeLength;
        }

        /// <summary>
        /// Obtiene la longitud actual de la cuerda
        /// </summary>
        public float GetCurrentLength()
        {
            return GetCurrentRopeLength();
        }

        /// <summary>
        /// Obtiene la dirección de tensión actual
        /// </summary>
        public Vector2 GetTensionDirection()
        {
            return tensionDirection;
        }

      
        /// Reinicializa la cuerda
        
        public void ResetRope()
        {
            InitializeRope();
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug || ropeSegments == null || ropeSegments.Count == 0) return;

            // Dibujar segmentos
            Gizmos.color = Color.yellow;
            for (int i = 0; i < ropeSegments.Count - 1; i++)
            {
                Gizmos.DrawLine(
                    ropeSegments[i].CurrentPosition,
                    ropeSegments[i + 1].CurrentPosition
                );
            }

            // Dibujar anclajes
            if (anchorStart && ropeStartTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(ropeStartTransform.position, 0.3f);
            }

            if (anchorEnd && ropeEndTransform != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(ropeEndTransform.position, 0.3f);
            }
        }

        #endregion
    }
}