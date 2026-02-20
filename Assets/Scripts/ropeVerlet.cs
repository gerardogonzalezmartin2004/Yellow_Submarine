using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

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

    private LineRenderer lineRenderer;
    private List<RopeSegment> ropeSegments = new List<RopeSegment>();

    [SerializeField] private Vector3 ropeStartPoint;
    [SerializeField] private Transform ropeStartTransform;

    private void Awake()
    {
        ropeStartPoint = new Vector3(ropeStartTransform.position.x, ropeStartTransform.position.y, ropeStartTransform.position.z);
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = numOfRopeSegments;

        for(int i = 0; i < numOfRopeSegments; i++)
        {
            ropeSegments.Add(new RopeSegment(ropeStartPoint));
            ropeStartPoint.y = ropeSegmentLength;

        }
    }

    private void Update()
    {
        DrawRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        for(int i = 0; i<numOfConstraintRuns; i++)
        {
            ApplyConstraints();

            if(i% collisionSegmentInterval == 0)
            {
                HandleCollisions();
            }
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
        RopeSegment firstSegment = ropeSegments[0];
        firstSegment.CurrentPosition = ropeStartTransform.position;
        ropeSegments[0] = firstSegment;

        for(int i = 0; i < numOfRopeSegments -1; i++)
        {
            RopeSegment currentSeg = ropeSegments[i];
            RopeSegment nextSeg = ropeSegments[i + 1];

            float dist = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).magnitude;
            float difference = (dist - ropeSegmentLength);

            Vector2 changeDir = (currentSeg.CurrentPosition - nextSeg.CurrentPosition).normalized;
            Vector2 changeVector = changeDir * difference;

            if(i != 0)
            {
                currentSeg.CurrentPosition -= (changeVector * .5f);
                nextSeg.CurrentPosition += (changeVector * .5f);
            }
            else
            {
                nextSeg.CurrentPosition += changeVector;
            }

            ropeSegments[i] = currentSeg;
            ropeSegments[i + 1] = nextSeg;
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
