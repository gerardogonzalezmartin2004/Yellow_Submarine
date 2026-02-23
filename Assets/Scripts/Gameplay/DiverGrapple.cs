using UnityEngine;
using System.Collections.Generic;
using AbyssalReach.Core;
using AbyssalReach.Data;

namespace AbyssalReach.Gameplay
{
    //Gestión del gancho físicas de springJoint, detección y gestión de peso
    [RequireComponent(typeof(Rigidbody))]
    public class DiverGrapple : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Radio de detección de objetos cercanos")]
        [SerializeField] private float detectionRadius = 3f;

        [Tooltip("Layer de los objetos recolectables")]
        [SerializeField] private LayerMask lootLayer;

        [Header("Capacity Limits (Upgradeable)")]
        [Tooltip("Máximo número de objetos que puedes arrastrar a la vez")]
        [SerializeField] private int maxCarriedCount = 3;

        [Tooltip("Peso máximo total que puedes cargar (kg)")]
        [SerializeField] private float maxWeightCapacity = 10f;

        [Header("Spring Joint Settings")]
        [Tooltip("Fuerza del resorte")]
        [SerializeField] private float springForce = 100f;

        [Tooltip("Amortiguación del resorte")]
        [SerializeField] private float springDamper = 10f;

        [Tooltip("Distancia máxima del joint antes de romperse")]
        [SerializeField] private float maxDistance = 5f;

        [Tooltip("Distancia a la que el objeto flota detrás del buzo")]
        [SerializeField] private float followDistance = 2f;

        [Header("References")]
        [Tooltip("Punto de anclaje del gancho (Transform hijo del buzo)")]
        [SerializeField] private Transform grappleAnchor;

        [Header("Visual - Line Renderer")]
        [Tooltip("Prefab del LineRenderer para los cables")]
        [SerializeField] private GameObject cableLinePrefab;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private Rigidbody rb;
        private AbyssalReachControls controls;

        // Listas paralelas: objetos, joints, y cables visuales
        private List<LootObject> carriedObjects;
        private List<SpringJoint> activeJoints;
        private List<GrappleLineRenderer> cableLines;

        [SerializeField, Tooltip("Peso actual transportado.")]
        private float currentWeight;

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            controls = new AbyssalReachControls();

            if (grappleAnchor == null)
            {
                grappleAnchor = transform;
                Debug.LogWarning("[DiverGrapple] Grapple Anchor no asignado, usando transform del buzo");
            }
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.DiverControls.Enable();

            controls.DiverControls.Interact.performed += OnGrabPressed;
            controls.DiverControls.Cancel.performed += OnReleasePressed;
        }

        private void OnDisable()
        {
            controls.DiverControls.Interact.performed -= OnGrabPressed;
            controls.DiverControls.Cancel.performed -= OnReleasePressed;

            controls.DiverControls.Disable();
            controls.Disable();
        }

        #endregion

        #region Input Callbacks
        // Estos métodos se llaman cuando el jugador presiona los botones de interactuar o cancelar.
        private void OnGrabPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            TryGrabNearestObject();
        }

        private void OnReleasePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            ReleaseAllObjects();
        }

        #endregion

        #region Detection & Grab Logic

        private void TryGrabNearestObject()
        {
            // Verificar si ya estamos al máximo de objetos o peso antes de intentar agarrar algo nuevo.
            if (IsFull())
            {
                if (showDebug) Debug.Log($"[DiverGrapple] ¡Lleno! Máximo: {maxCarriedCount}");
                return;
            }
            // Encontrar el objeto más cercano dentro del radio de detección.
            LootObject target = FindNearestLootObject();

            LootItemData itemData = target.GetItemData();
            if (itemData == null)
            {
                Debug.LogWarning("[DiverGrapple] El objeto no tiene ItemData");
                return;
            }
            // Verificar si el nuevo peso total sobrepasa la capacidad máxima antes de agarrar el objeto.
            float newTotalWeight = currentWeight + itemData.weight;
            if (newTotalWeight > maxWeightCapacity)
            {
               
                Debug.Log("Demasiado pesado");
                return;
            }

            GrabObject(target, itemData.weight);
        }

        private LootObject FindNearestLootObject()
        {
            // Usar OverlapSphere para encontrar todos los colliders de objetos recolectables dentro del radio, luego determinar cuál es el más cercano.
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, lootLayer);
            LootObject nearest = null;
            float closestDistance = Mathf.Infinity;
            // Iterar sobre los colliders encontrados para encontrar el más cercano que no esté ya siendo llevado.
            foreach (Collider col in hits)
            {
                LootObject loot = col.GetComponent<LootObject>();

                if (loot == null || carriedObjects.Contains(loot)) continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearest = loot;
                }
            }

            return nearest;
        }

        private void GrabObject(LootObject loot, float weightToAdd)
        {
            //  Crear el SpringJoint
            SpringJoint joint = gameObject.AddComponent<SpringJoint>();
            joint.connectedBody = loot.GetRigidbody();
            joint.anchor = Vector3.zero;
            joint.spring = springForce;
            joint.damper = springDamper;
            joint.maxDistance = maxDistance;
            joint.minDistance = followDistance;
            joint.autoConfigureConnectedAnchor = true;

            //  Crear el cable visual
            GrappleLineRenderer cableLine = CreateCableLine(grappleAnchor, loot.transform);

            //  Actualizar listas y peso
            carriedObjects.Add(loot);
            activeJoints.Add(joint);
            cableLines.Add(cableLine);
            currentWeight += weightToAdd; 

            loot.SetGrabbed(true);           
        }

        private GrappleLineRenderer CreateCableLine(Transform origin, Transform target)
        {
            // Este método crea un nuevo objeto para el cable visual, le asigna el prefab si está disponible, y configura el GrappleLineRenderer con los puntos de origen y destino.
            GameObject cableObj;

            if (cableLinePrefab != null)
            {
                cableObj = Instantiate(cableLinePrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                cableObj = new GameObject("CableLine");
                cableObj.AddComponent<LineRenderer>();
                cableObj.AddComponent<GrappleLineRenderer>();
                Debug.LogWarning("[DiverGrapple] Cable Line Prefab no asignado - creando dinámicamente");
            }

            GrappleLineRenderer cableLine = cableObj.GetComponent<GrappleLineRenderer>();
            if (cableLine != null)
            {
                cableLine.SetTargets(origin, target);
                cableLine.SetVisible(true);
            }

            return cableLine;
        }

        #endregion

        #region Release Logic

        private void ReleaseAllObjects()
        {
            if (carriedObjects.Count == 0)
            {
                return;
            }
                

             ClearGrappleData(false);
        }

        public List<LootObject> CollectCarriedObjects()
        {
            // Este método se llama cuando el buzo llega a la zona de entrega para recoger los objetos que está llevando. Devuelve la lista de objetos recolectados y luego limpia las listas y destruye los joints y cables.
            List<LootObject> collected = new List<LootObject>(carriedObjects);
            ClearGrappleData(true);

            
            return collected;
        }

        
        private void ClearGrappleData(bool isBeingCollected)
        {
            // Destruir todos los SpringJoints y cables visuales, y actualizar el estado de los objetos llevados si no están siendo recolectados (es decir, si solo se están soltando).
            foreach (SpringJoint joint in activeJoints) Destroy(joint);
            foreach (GrappleLineRenderer cable in cableLines) Destroy(cable.gameObject);

            if (!isBeingCollected)
            {
                foreach (LootObject loot in carriedObjects)
                {
                    if (loot != null) loot.SetGrabbed(false);
                }
            }

            activeJoints.Clear();
            cableLines.Clear();
            carriedObjects.Clear();
            currentWeight = 0f;
        }

        #endregion
        #region API Pública (Upgrades)

        public void UpgradeMaxCount(int newMax)
        { // Este método se puede llamar desde el sistema de mejoras para aumentar la cantidad máxima de objetos que el buzo puede llevar.
            if (newMax > maxCarriedCount)
            {
                maxCarriedCount = newMax;

                if (showDebug)
                {
                    Debug.Log("[DiverGrapple] Capacidad mejorada a " + maxCarriedCount + " objetos");
                }
            }
        }

        public void UpgradeMaxWeight(float newMax)
        {
            // Este método se puede llamar desde el sistema de mejoras para aumentar la capacidad de peso del gancho.
            if (newMax > maxWeightCapacity)
            {
                maxWeightCapacity = newMax;

                if (showDebug)
                {
                    Debug.Log("[DiverGrapple] Peso máximo mejorado a " + maxWeightCapacity + "kg");
                }
            }
        }

        public int GetCarriedCount()
        {
            return carriedObjects.Count;
        }

        public float GetCurrentWeight()
        {
            return currentWeight;
        }

        public int GetMaxCarriedCount()
        {
            return maxCarriedCount;
        }

        public float GetMaxWeightCapacity()
        {
            return maxWeightCapacity;
        }

        public bool IsFull()
        {
            if (carriedObjects.Count >= maxCarriedCount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsOverWeight()
        {
            if (currentWeight >= maxWeightCapacity)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            if (!showDebug)
            {
                return;
            }

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        

        #endregion
    }
}