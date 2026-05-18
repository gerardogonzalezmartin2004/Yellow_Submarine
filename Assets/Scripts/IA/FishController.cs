using UnityEngine;
using IA.Movement.Combination;
using IA26.Movement.Kinematic;
using IA26.Movement.Dinamic;

// Ponlo en cada prefab de pez junto a BlendedSteering, KSeek, KFlee y DSeparation.
public class FishController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BlendedSteering blendedSteering;
    [SerializeField] private Transform diverTransform;

    [Header("Detección del diver")]
    [SerializeField] private float detectionRadius = 4f;
    [SerializeField] private float calmRadius = 6f; // radio para volver a la calma

    [Header("Waypoints (estado normal)")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float waypointChangeInterval = 3f;

    [Header("Pesos BlendedSteering")]
    [Range(0, 1)][SerializeField] private float seekWeight = 0.6f;
    [Range(0, 1)][SerializeField] private float fleeWeight = 0.8f;
    [Range(0, 1)][SerializeField] private float separationWeight = 0.3f;

    private enum FishState { Normal, Scared }
    private FishState currentState = FishState.Normal;

    private Transform waypointTarget;
    private float waypointTimer;
    private Vector2 spawnCenter;

    private void Awake()
    {
        // Crear un Transform vacío como waypoint
        waypointTarget = new GameObject($"{gameObject.name}_Waypoint").transform;
        spawnCenter = transform.position;
        SetNewWaypoint();
    }

    private void Start()
    {
        // Buscar al diver si no está asignado
        if (diverTransform == null)
        {
            GameObject diver = GameObject.FindGameObjectWithTag("Player");
            if (diver != null) diverTransform = diver.transform;
        }

        // Asignar target del KSeek y KFlee
        KSeek seek = GetComponent<KSeek>();
        KFlee flee = GetComponent<KFlee>();

        if (seek != null) SetTarget(seek, waypointTarget);
        if (flee != null && diverTransform != null) SetTarget(flee, diverTransform);

        SetState(FishState.Normal);
    }

    private void Update()
    {
        UpdateState();

        // Cambiar waypoint periódicamente en estado normal
        if (currentState == FishState.Normal)
        {
            waypointTimer -= Time.deltaTime;
            if (waypointTimer <= 0f ||
                Vector2.Distance(transform.position, waypointTarget.position) < waypointReachDistance)
            {
                SetNewWaypoint();
            }
        }
    }

    private void UpdateState()
    {
        if (diverTransform == null) return;

        float dist = Vector2.Distance(transform.position, diverTransform.position);

        if (currentState == FishState.Normal && dist < detectionRadius)
            SetState(FishState.Scared);
        else if (currentState == FishState.Scared && dist > calmRadius)
            SetState(FishState.Normal);
    }

    private void SetState(FishState newState)
    {
        currentState = newState;

        if (newState == FishState.Normal)
        {
            // Seek al waypoint, sin flee
            blendedSteering.SetWeight<KSeek>(seekWeight);
            blendedSteering.SetWeight<KFlee>(0f);
            blendedSteering.SetWeight<DSeparation>(separationWeight);
        }
        else // Scared
        {
            // Flee del diver, seek desactivado
            blendedSteering.SetWeight<KSeek>(0f);
            blendedSteering.SetWeight<KFlee>(fleeWeight);
            blendedSteering.SetWeight<DSeparation>(separationWeight);
        }
    }

    private void SetNewWaypoint()
    {
        Vector2 randomPos = spawnCenter + Random.insideUnitCircle * wanderRadius;
        waypointTarget.position = randomPos;
        waypointTimer = waypointChangeInterval;
    }

    // Reflection helper para asignar target a KSeek/KFlee desde código
    private void SetTarget(KSeek behavior, Transform t)
    {
        var field = typeof(KSeek).GetField("target",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(behavior, t);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, calmRadius);
    }
}