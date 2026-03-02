using AbyssalReach.Core;
using AbyssalReach.Gameplay;
using System.Collections.Generic;
using UnityEngine;

public class ropeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int numOfRopeSegments = 50;
    [SerializeField] private float ropeSegmentLength = .225f;

    [Header("Physics")]
    [SerializeField] private Vector2 gravityForce = new Vector2(0f, -1f);
    [SerializeField] private float dampingFactor = .98f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = .1f;
    [SerializeField] private float bounceFactor = .1f;

    [Header("Constraints")]
    [SerializeField] private int numOfConstraintRuns = 80;   // ← subido bastante (prueba 100-150)

    [Header("Optimizations")]
    [SerializeField] private int collisionSegmentInterval = 2;

    [Header("Tension")]
    [SerializeField] private float tensionStrength = 25f;

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    [SerializeField] private Vector3 ropeStartPoint;
    [SerializeField] private Transform ropeStartTransform;
    [SerializeField] private Transform ropeEndTransform;
    [SerializeField] private bool anchorStart = true;
    [SerializeField] private bool anchorEnd = true;

    public Vector2 tensionDir;

    [SerializeField] private TetherSystem tetherSystem;

    private GameController gameController;

    private void Awake()
    {
        gameController = GameController.Instance;

        ropeStartPoint = ropeStartTransform.position;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = numOfRopeSegments;

        CalculateLength();

        for (int i = 0; i < numOfRopeSegments; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= ropeSegmentLength;
        }
    }

    private void Update()
    {
        DrawRope();
        CalculateLength();
    }

    public void CalculateLength()
    {
        if (tetherSystem != null)
        {
            ropeSegmentLength = tetherSystem.GetMaxLength() / (numOfRopeSegments - 1);
        }
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
    }

    private float GetCurrentRopeLength()
    {
        float length = 0f;
        for (int i = 0; i < ropeSegments.Count - 1; i++)
        {
            length += Vector2.Distance(ropeSegments[i].CurrentPosition, ropeSegments[i + 1].CurrentPosition);
        }
        return length;
    }

    private void ApplyTensionForce()
    {
        if (ropeEndTransform == null) return;

        Rigidbody rb = ropeEndTransform.GetComponent<Rigidbody>();
        if (rb == null) return;

        bool isEmergency = gameController != null && gameController.IsEmergencyAscent();

        float currentLength = GetCurrentRopeLength();
        float totalLength = (numOfRopeSegments - 1) * ropeSegmentLength;

        // Activar si está casi estirado o en emergencia
        if (currentLength < totalLength * 0.92f && !isEmergency) return;

        Vector2 tensionDir = Vector2.up; // fallback

        if (ropeSegments.Count >= 2)
        {
            int last = ropeSegments.Count - 1;
            int prev = last - 1;

            Vector2 diverPos = ropeSegments[last].CurrentPosition;
            Vector2 prevPos = ropeSegments[prev].CurrentPosition;

            tensionDir = (prevPos - diverPos).normalized;
            this.tensionDir = tensionDir;

            // Debug: dirección de tensión (debería seguir la cuerda)
            Debug.DrawRay(diverPos, tensionDir * 6f, Color.red, 0.15f);
        }

        float force = tensionStrength;

        if (isEmergency)
        {
            force *= 18f;           // ← valor alto — prueba entre 12–30
            rb.linearDamping = 0.4f; // ← muy bajo en emergencia
        }
        else
        {
            rb.linearDamping = 3f;   // valor normal
        }

        // Aplicamos fuerza siguiendo la dirección de la cuerda
        rb.AddForce(tensionDir * force, ForceMode.VelocityChange);
        // Alternativa más suave si VelocityChange es demasiado brusco:
        // rb.AddForce(tensionDir * force * Time.fixedDeltaTime * 60f, ForceMode.Force);
    }

    private void DrawRope()
    {
        Vector3[] ropePositions = new Vector3[numOfRopeSegments];
        for (int i = 0; i < ropeSegments.Count; i++)
        {
            ropePositions[i] = ropeSegments[i].CurrentPosition;
        }
        lineRenderer.SetPositions(ropePositions);
    }

    private void Simulate()
    {
        for (int i = 0; i < ropeSegments.Count; i++)
        {
            RopeSegment segment = ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * dampingFactor;
            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += gravityForce * Time.fixedDeltaTime;
            ropeSegments[i] = segment;
        }
    }

    private void ApplyConstraints()
    {
        // Ancla inicial (barco)
        RopeSegment first = ropeSegments[0];
        first.CurrentPosition = ropeStartTransform.position;
        first.OldPosition = ropeStartTransform.position;
        ropeSegments[0] = first;

        // Ancla final → SIEMPRE pegado al diver
        int lastIndex = ropeSegments.Count - 1;
        RopeSegment last = ropeSegments[lastIndex];
        last.CurrentPosition = (Vector2)tetherSystem.GetDiverAnchor().position;
        // Opcional: si quieres resetear OldPosition también (puede ayudar a evitar jitter)
        // last.OldPosition = last.CurrentPosition;
        ropeSegments[lastIndex] = last;

        // Constraints de distancia (más iteraciones = más rígido)
        for (int i = 0; i < ropeSegments.Count - 1; i++)
        {
            RopeSegment a = ropeSegments[i];
            RopeSegment b = ropeSegments[i + 1];

            Vector2 delta = b.CurrentPosition - a.CurrentPosition;
            float dist = delta.magnitude;

            if (dist == 0) continue;

            float error = dist - ropeSegmentLength;
            Vector2 correction = delta.normalized * error * 0.5f;

            if (i == 0)
            {
                b.CurrentPosition -= correction * 1.0f;
            }
            else if (i == ropeSegments.Count - 2)
            {
                a.CurrentPosition += correction * 1.0f;
            }
            else
            {
                a.CurrentPosition += correction * 0.5f;
                b.CurrentPosition -= correction * 0.5f;
            }

            ropeSegments[i] = a;
            ropeSegments[i + 1] = b;
        }
    }

    private void HandleCollisions()
    {
        // ... (mantengo igual tu implementación original de colisiones)
        for (int i = 1; i < ropeSegments.Count; i++)
        {
            RopeSegment segment = ropeSegments[i];
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

    public struct RopeSegment
    {
        public Vector2 CurrentPosition;
        public Vector2 OldPosition;

        public RopeSegment(Vector2 pos)
        {
            CurrentPosition = pos;
            OldPosition = pos;
        }
    }
}