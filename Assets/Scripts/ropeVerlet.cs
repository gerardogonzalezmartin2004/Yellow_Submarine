using AbyssalReach.Gameplay;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ropeVerlet;

public class ropeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int numOfRopeSegments = 50;
    [SerializeField] private float ropeSegmentLength = .225f;

    [Header("Physics")]
    [SerializeField] private Vector2 gravityForce = new Vector2(0f, -1f);
    [SerializeField] private float dampìngFactor = .98f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = .1f;
    [SerializeField] private float bounceFactor = .1f;
    [SerializeField] private float correctionClampAmount = .1f;

    [Header("Constraints")]
    [SerializeField] private int numOfConstraintRuns = 50;

    [Header("Optimizations")]
    [SerializeField] private int collisionSegmentInterval = 2;

    [Header("Length Limit")]
    [SerializeField] private float maxRopeLength = 10f;

    [Header("Tension")]
    [SerializeField] private float tensionStrength = 25f;

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    [SerializeField] private Vector3 ropeStartPoint;
    [SerializeField] private Transform ropeStartTransform;
    [SerializeField] private Transform ropeEndTransform;
    [SerializeField] private bool anchorStart = true;
    [SerializeField] private bool anchorEnd = true;
    [SerializeField] private TetherSystem tetherSystem;

    private void Awake()
    {
        ropeStartPoint = ropeStartTransform.position;
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = numOfRopeSegments;

        // Calcular longitud de cada segmento según el tether
        if (tetherSystem != null)
        {
            ropeSegmentLength = tetherSystem.GetMaxLength() / (numOfRopeSegments - 1);
        }

        for (int i = 0; i < numOfRopeSegments; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y -= ropeSegmentLength; // baja los segmentos
        }
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
    }

    private float GetCurrentRopeLength()
    {
        float length = 0f;

        for (int i = 0; i < ropeSegments.Count - 1; i++)
        {
            length += Vector2.Distance(
                ropeSegments[i].CurrentPosition,
                ropeSegments[i + 1].CurrentPosition);
        }

        return length;
    }


    private void ApplyTensionForce()
    {
        if (ropeEndTransform == null) return;

        Rigidbody rb = ropeEndTransform.GetComponent<Rigidbody>();
        if (rb == null) return;

        float totalLength = (numOfRopeSegments - 1) * ropeSegmentLength;

        float currentLength = 0f;

        for (int i = 0; i < ropeSegments.Count - 1; i++)
        {
            currentLength += Vector2.Distance(
                ropeSegments[i].CurrentPosition,
                ropeSegments[i + 1].CurrentPosition);
        }

        // Si está prácticamente estirada al máximo
        if (currentLength >= totalLength * 0.98f)
        {
            Vector2 dir = (ropeStartTransform.position - ropeEndTransform.position).normalized;

            rb.AddForce(dir * tensionStrength, ForceMode.Force);
        }
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
        for(int i = 0;i < ropeSegments.Count; i++)
        {
            RopeSegment segment = ropeSegments[i];
            Vector2 velocity = (segment.CurrentPosition - segment.OldPosition) * dampìngFactor;

            segment.OldPosition = segment.CurrentPosition;
            segment.CurrentPosition += velocity;
            segment.CurrentPosition += gravityForce * Time.fixedDeltaTime;
            ropeSegments[i] = segment;
        }
    }


    private void ApplyConstraints()
    {
        // Inicio al barco
        RopeSegment first = ropeSegments[0];
        first.CurrentPosition = ropeStartTransform.position;
        ropeSegments[0] = first;

        // Final al diver
        RopeSegment last = ropeSegments[ropeSegments.Count - 1];
        last.CurrentPosition = tetherSystem.GetDiverAnchor().position; // sincroniza con diver real
        ropeSegments[ropeSegments.Count - 1] = last;

        // Ajuste de todos los segmentos intermedios
        for (int i = 0; i < ropeSegments.Count - 1; i++)
        {
            RopeSegment current = ropeSegments[i];
            RopeSegment next = ropeSegments[i + 1];

            float dist = (current.CurrentPosition - next.CurrentPosition).magnitude;
            float diff = dist - ropeSegmentLength;

            Vector2 dir = (current.CurrentPosition - next.CurrentPosition).normalized;
            Vector2 offset = dir * diff;

            if (i == 0)
                next.CurrentPosition += offset; // solo mover siguiente
            else if (i == ropeSegments.Count - 2)
                current.CurrentPosition -= offset; // solo mover actual
            else
            {
                current.CurrentPosition -= offset * 0.5f;
                next.CurrentPosition += offset * 0.5f;
            }

            ropeSegments[i] = current;
            ropeSegments[i + 1] = next;
        }
    }

    private void HandleCollisions()
    {
        for(int i = 1; i < ropeSegments.Count; i++)
        {
            RopeSegment segment = ropeSegments[i];
            Vector2 velocity = segment.CurrentPosition - segment.OldPosition;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(segment.CurrentPosition, collisionRadius, collisionMask);

            foreach(Collider2D collider in colliders)
            {
                Vector2 closestPoint = collider.ClosestPoint(segment.CurrentPosition);
                float distance = Vector2.Distance(segment.CurrentPosition, closestPoint);

                //si esta en el radio de colision, mueve la cuerda
                if(distance < collisionRadius)
                {
                    Vector2 normal = (segment.CurrentPosition - closestPoint).normalized;
                    if(normal == Vector2.zero)
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
