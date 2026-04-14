using AbyssalReach.Core;
using UnityEngine;
using System.Collections.Generic;

namespace AbyssalReach.Gameplay
{

    // Sistema de cuerda con límite de distancia ESCALABLE


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
        [SerializeField] private int numOfRopeSegments = 50;
        [SerializeField] private float ropeSegmentLength = 0.225f;

        [Header("Physics")]
        [SerializeField] private Vector2 gravityForce = new Vector2(0f, -1f);
        [SerializeField] private float dampingFactor = 0.98f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float collisionRadius = 0.1f;
        [SerializeField] private float bounceFactor = 0.1f;
        [SerializeField] private int collisionSegmentInterval = 2;

        [Header("Constraints")]
        [SerializeField] private int numOfConstraintRuns = 80;

        [Header("Tension Force")]
        [SerializeField] private float tensionStrength = 25f;
        [SerializeField] private float emergencyForceMultiplier = 20f;
        [SerializeField] private float tensionThreshold = 0.92f;

        [Header("Anchors")]
        [SerializeField] private Transform ropeStartTransform;
        [SerializeField] private Transform ropeEndTransform;
        [SerializeField] private bool anchorStart = true;
        [SerializeField] private bool anchorEnd = true;





        [Header("Distance Limit (Upgradeable)")]
        [Tooltip("Distancia máxima inicial (se puede upgradear)")]
        [SerializeField] private float maxDistance = 30f;

        [Tooltip("Fuerza de frenado al alcanzar límite")]
        [SerializeField] private float limitForce = 500f;
        private bool enableDistanceLimit = true;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;
        [SerializeField] private bool showTensionDirection = true;

        #endregion

        #region Private Fields

        private LineRenderer lineRenderer;
        private List<ropeSegment> ropeSegments = new List<ropeSegment>();
        private GameController gameController;
        private Rigidbody2D diverRb2D;


        private float activeRopeLength = 30f;
        private Vector2 tensionDirection = Vector2.up;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            gameController = GameController.Instance;
            lineRenderer = GetComponent<LineRenderer>();

            if (ropeEndTransform != null)
            {
                diverRb2D = ropeEndTransform.GetComponent<Rigidbody2D>();

                if (diverRb2D == null)
                {
                    Debug.LogError("[RopeVerlet]  Diver NO tiene Rigidbody2D!");
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

                if (i % collisionSegmentInterval == 0)
                {
                    HandleCollisions();
                }
            }

            // Aplicar límite de distancia
            EnforceDistanceLimit();
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

            activeRopeLength = maxDistance;
            CalculateSegmentLength();

            Vector2 startPos = ropeStartTransform.position;
            ropeSegments.Clear();

            for (int i = 0; i < numOfRopeSegments; i++)
            {
                ropeSegments.Add(new ropeSegment(startPos));
                startPos.y -= ropeSegmentLength;
            }

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
            if (anchorStart && ropeStartTransform != null)
            {
                ropeSegment first = ropeSegments[0];
                first.CurrentPosition = ropeStartTransform.position;
                first.OldPosition = ropeStartTransform.position;
                ropeSegments[0] = first;
            }

            if (anchorEnd && ropeEndTransform != null)
            {
                int lastIndex = ropeSegments.Count - 1;
                ropeSegment last = ropeSegments[lastIndex];
                last.CurrentPosition = ropeEndTransform.position;
                ropeSegments[lastIndex] = last;
            }

            for (int i = 0; i < ropeSegments.Count - 1; i++)
            {
                ropeSegment a = ropeSegments[i];
                ropeSegment b = ropeSegments[i + 1];
                Vector2 delta = b.CurrentPosition - a.CurrentPosition;
                float dist = delta.magnitude;

                if (dist < 0.0001f) continue;

                float error = dist - ropeSegmentLength;
                Vector2 correction = delta.normalized * error * 0.5f;

                if (i == 0 && anchorStart)
                {
                    b.CurrentPosition -= correction * 2f;
                }
                else if (i == ropeSegments.Count - 2 && anchorEnd)
                {
                    a.CurrentPosition += correction * 2f;
                }
                else
                {
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
            int startIndex = anchorStart ? 1 : 0;
            int endIndex = anchorEnd ? ropeSegments.Count - 1 : ropeSegments.Count;

            for (int i = startIndex; i < endIndex; i++)
            {
                ropeSegment segment = ropeSegments[i];
                Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
                Collider2D[] colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, collisionRadius, collisionMask);

                foreach (Collider2D collider in colliders)
                {
                    Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                    float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                    if (distance < collisionRadius)
                    {
                        Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;
                        if (normal == Vector2.zero)
                        {
                            normal = (segment.CurrentPosition - (Vector2)collider.transform.position).normalized;
                        }

                        float depth = collisionRadius - distance;
                        segment.CurrentPosition += normal * depth;
                        velocity = Vector2.Reflect(velocity, normal) * bounceFactor;
                    }
                }

                segment.OldPosition = segment.CurrentPosition - velocity;
                ropeSegments[i] = segment;
            }
        }

        #endregion


        #region Distance Limit


        // Aplica el límite de distancia máxima
        // Frena al diver si intenta alejarse más de maxDistance

        private void EnforceDistanceLimit()
        {
            if (!enableDistanceLimit) return;
            if (diverRb2D == null) return;
            if (ropeStartTransform == null || ropeEndTransform == null) return;

            // Medir distancia directa barco - diver
            Vector2 boatPos = ropeStartTransform.position;
            Vector2 diverPos = ropeEndTransform.position;
            float currentDistance = Vector2.Distance(boatPos, diverPos);

            // Si excede el límite
            if (currentDistance > maxDistance)
            {
                Vector2 toBoat = (boatPos - diverPos).normalized;
                float excess = currentDistance - maxDistance;

                // Aplicar fuerza de retorno
                float force = limitForce * excess;
                diverRb2D.AddForce(toBoat * force, ForceMode2D.Force);

                // Cancelar velocidad de alejamiento
                Vector2 velocity = diverRb2D.linearVelocity;
                float awaySpeed = Vector2.Dot(velocity, -toBoat);

                if (awaySpeed > 0f)
                {
                    Vector2 awayVelocity = -toBoat * awaySpeed;
                    diverRb2D.linearVelocity -= awayVelocity * 0.8f;
                }

                if (showDebug)
                {
                    Debug.LogWarning($"🛑 LÍMITE! {currentDistance:F2}m / {maxDistance:F2}m (exceso: {excess:F2}m)");
                }
            }
        }

        #endregion


        #region Tension Force

        private void ApplyTensionForce()
        {
            if (diverRb2D == null || ropeSegments.Count < 2) return;

            bool isEmergency = gameController != null && gameController.IsEmergencyAscent();
            float currentLength = GetCurrentRopeLength();
            float totalLength = (numOfRopeSegments - 1) * ropeSegmentLength;

            if (currentLength < totalLength * tensionThreshold && !isEmergency)
            {
                return;
            }

            CalculateTensionDirection();
            float force = tensionStrength;

            if (isEmergency)
            {
                force *= emergencyForceMultiplier;
                diverRb2D.linearDamping = 0f;
                diverRb2D.mass = 0.5f;
            }
            else
            {
                diverRb2D.linearDamping = 0f;
                diverRb2D.mass = 10f;
            }

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

                if (showTensionDirection)
                {
                    Debug.DrawRay(diverPos, tensionDirection * 6f, Color.red, 0.15f);
                }
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
            activeRopeLength = Mathf.Max(1f, activeRopeLength - amount);
            CalculateSegmentLength();
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


        // Establece la longitud máxima del cable (para upgrades)

        public void SetMaxLength(float length)
        {
            maxDistance = length;
            activeRopeLength = length;
            CalculateSegmentLength();

            if (showDebug)
            {
                Debug.Log($"[RopeVerlet] ⬆️ Límite actualizado a {length}m");
            }
        }

        // Obtiene la longitud máxima actual
        public float GetMaxLength()
        {
            return maxDistance;
        }

        // Obtiene la longitud actual de la cuerda
        public float GetCurrentLength()
        {
            return GetCurrentRopeLength();
        }

        // Obtiene la distancia directa barco - diver
        public float GetDirectDistance()
        {
            if (ropeStartTransform == null || ropeEndTransform == null)
                return 0f;

            return Vector2.Distance(ropeStartTransform.position, ropeEndTransform.position);
        }

        // Verifica si está al límite
        public bool IsAtDistanceLimit()
        {
            return GetDirectDistance() >= maxDistance * 0.95f;
        }

        public Vector2 GetTensionDirection()
        {
            return tensionDirection;
        }

        public void ResetRope()
        {
            InitializeRope();
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // Dibujar segmentos
            if (ropeSegments != null && ropeSegments.Count > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < ropeSegments.Count - 1; i++)
                {
                    Gizmos.DrawLine(
                        ropeSegments[i].CurrentPosition,
                        ropeSegments[i + 1].CurrentPosition
                    );
                }
            }

            // Dibujar anclajes
            if (anchorStart && ropeStartTransform != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(ropeStartTransform.position, 0.3f);

                // Dibujar círculo de límite de distancia
                if (enableDistanceLimit)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    DrawCircle(ropeStartTransform.position, maxDistance);

                    // Texto de distancia actual
                    if (ropeEndTransform != null)
                    {
                        float dist = Vector2.Distance(ropeStartTransform.position, ropeEndTransform.position);
                        bool atLimit = dist >= maxDistance * 0.95f;
                        Gizmos.color = atLimit ? Color.red : Color.yellow;
                        Gizmos.DrawLine(ropeStartTransform.position, ropeEndTransform.position);
                    }
                }
            }

            if (anchorEnd && ropeEndTransform != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(ropeEndTransform.position, 0.3f);
            }
        }

        private void DrawCircle(Vector3 center, float radius)
        {
            int segments = 36;
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