using AbyssalReach.Core;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalReach.Gameplay
{
    public class PortArea : MonoBehaviour
    {
        [Header("Detection Zones")]
        [SerializeField] private float outerRadius = 15f;
        [SerializeField] private float innerRadius = 3f;

        [Header("Docking")]
        [SerializeField] private Transform dockingPoint;
        [SerializeField] private float dockingSpeed = 2f;
        [SerializeField] private float arrivalThreshold = 0.5f;
        [SerializeField] private float exitCooldown = 2f;

        [Header("References")]
        [SerializeField] private string targetTag = "Boat";
        [SerializeField] private GameObject shopUIPanel;

        [Header("Loot Summary")]
        [Tooltip("Script LootSummaryUI — muestra los items del grid del barco al entrar al puerto.")]
        [SerializeField] private LootSummaryUI lootSummaryUI;

        [Tooltip("ItemGrid del barco — fuente de los items a mostrar en el resumen.")]
        [SerializeField] private ItemGrid boatItemGrid;

        [Header("UI Messages")]
        [SerializeField] private string dockingMessage = "Press 'E' to Dock";
        [SerializeField] private string autoPilotMessage = "Auto-Pilot Active...";

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color outerGizmoColor = new Color(0f, 1f, 0.5f, 0.2f);
        [SerializeField] private Color innerGizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

        // ── Estado interno ──
        private bool boatInOuterZone = false;
        private bool isAutoPiloting = false;
        private bool isInShop = false;
        private bool isExiting = false;
        private float exitTimer = 0f;
        private float dockingTravelTime = 0f;

        private GameObject detectedBoat;
        private Rigidbody boatRb;
        private BoatMovement boatMovement;

        private AbyssalReachControls controls;

        #region Unity Lifecycle

        private void Awake()
        {
            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.Enable();
            controls.BoatControls.Interact.performed += OnDockPressed;
            controls.UI.Enable();
            controls.UI.Cancel.performed += OnCancelPressed;
        }

        private void OnDisable()
        {
            controls.BoatControls.Interact.performed -= OnDockPressed;
            controls.UI.Cancel.performed -= OnCancelPressed;
            controls.BoatControls.Disable();
            controls.Disable();
        }

        private void Update()
        {
            UpdateCooldown();
            CheckBoatInZones();
            if (isAutoPiloting) UpdateAutoPilot();
        }

        #endregion

        #region Input

        private void OnDockPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (boatInOuterZone && !isAutoPiloting && !isInShop)
                StartAutoPilot();
        }

        private void OnCancelPressed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (isInShop) CloseShop();
        }

        #endregion

        #region Zone Detection

        private void CheckBoatInZones()
        {
            GameObject boat = GameObject.FindGameObjectWithTag(targetTag);
            if (boat == null) { boatInOuterZone = false; detectedBoat = null; return; }

            float distance = Vector3.Distance(transform.position, boat.transform.position);

            if (!isExiting)
            {
                bool was = boatInOuterZone;
                boatInOuterZone = distance <= outerRadius;

                if (boatInOuterZone && !was) OnBoatEnterOuterZone(boat);
                else if (!boatInOuterZone && was) OnBoatExitOuterZone();
            }
        }

        private void OnBoatEnterOuterZone(GameObject boat)
        {
            detectedBoat = boat;
            boatRb = boat.GetComponent<Rigidbody>();
            boatMovement = boat.GetComponent<BoatMovement>();

            if (showDebug) Debug.Log("[PortArea] Barco en zona de detección");
            GameController.Instance?.EnterPort();
        }

        private void OnBoatExitOuterZone()
        {
            detectedBoat = null;
            if (isAutoPiloting) CancelAutoPilot();
            if (showDebug) Debug.Log("[PortArea] Barco salió de zona de detección");
            GameController.Instance?.ExitPort();
        }

        #endregion

        #region Auto-Pilot

        private void StartAutoPilot()
        {
            if (dockingPoint == null || detectedBoat == null) return;

            isAutoPiloting = true;
            boatMovement?.SetMovementActive(false);

            // Tiempo de viaje real = distancia / velocidad.
            float distance = Vector3.Distance(detectedBoat.transform.position, dockingPoint.position);
            dockingTravelTime = distance / Mathf.Max(dockingSpeed, 0.01f);

            if (showDebug)
                Debug.Log($"[PortArea] Auto-Pilot | distancia: {distance:F1}m | tiempo: {dockingTravelTime:F1}s");

            // ── Mostrar resumen de items del barco ──
            TriggerLootSummary(dockingTravelTime);
        }

        private void UpdateAutoPilot()
        {
            if (detectedBoat == null || dockingPoint == null) { CancelAutoPilot(); return; }

            float distance = Vector3.Distance(detectedBoat.transform.position, dockingPoint.position);
            if (distance <= arrivalThreshold) { ArriveAtDock(); return; }

            Vector3 dir = (dockingPoint.position - detectedBoat.transform.position).normalized;
            float speed = dockingSpeed * Mathf.Clamp01(distance / 5f);

            if (boatRb != null)
            {
                boatRb.MovePosition(boatRb.position + dir * speed * Time.deltaTime);
                boatRb.angularVelocity = Vector3.zero;
            }
        }

        private void ArriveAtDock()
        {
            isAutoPiloting = false;

            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
                boatRb.angularVelocity = Vector3.zero;
                boatRb.isKinematic = true;
            }

            boatMovement?.Stop();
            OpenShop();
            if (showDebug) Debug.Log("[PortArea] Barco atracado — tienda abierta");
        }

        private void CancelAutoPilot()
        {
            isAutoPiloting = false;
            boatMovement?.SetMovementActive(true);
            if (boatRb != null) boatRb.isKinematic = false;
            if (showDebug) Debug.Log("[PortArea] Auto-Pilot cancelado");
        }

        #endregion

        #region Loot Summary

        private void TriggerLootSummary(float travelTime)
        {
            if (lootSummaryUI == null)
            {
                Debug.LogWarning("[PortArea] lootSummaryUI no asignado.");
                return;
            }

            if (boatItemGrid == null)
            {
                Debug.LogWarning("[PortArea] boatItemGrid no asignado — no se pueden leer los items del barco.");
                return;
            }

            // Obtener items únicos del grid del barco.
            List<InventoryItem> items = boatItemGrid.GetAllItems();

            if (items == null || items.Count == 0)
            {
                if (showDebug) Debug.Log("[PortArea] Grid del barco vacío — sin resumen.");
                return;
            }

            if (showDebug)
                Debug.Log($"[PortArea] Resumen: {items.Count} items del barco en {travelTime:F1}s");

            lootSummaryUI.ShowLootSummary(items, travelTime);
        }

        #endregion

        #region Shop

        private void OpenShop()
        {
            if (shopUIPanel == null) { Debug.LogWarning("[PortArea] shopUIPanel no asignado"); return; }
            isInShop = true;
            shopUIPanel.SetActive(true);
            GameController.Instance?.SetGameState(GameController.GameState.InShop);
            if (showDebug) Debug.Log("[PortArea] Tienda abierta");
        }

        public void CloseShop()
        {
            if (shopUIPanel == null) return;
            isInShop = false;
            shopUIPanel.SetActive(false);
            boatMovement?.SetMovementActive(true);
            if (boatRb != null) boatRb.isKinematic = false;
            GameController.Instance?.ExitPort();
            StartExitCooldown();
            if (showDebug) Debug.Log("[PortArea] Tienda cerrada — cooldown activo");
        }

        #endregion

        #region Cooldown

        private void StartExitCooldown()
        {
            isExiting = true;
            exitTimer = exitCooldown;
            boatInOuterZone = false;
        }

        private void UpdateCooldown()
        {
            if (!isExiting) return;
            exitTimer -= Time.deltaTime;
            if (exitTimer <= 0f)
            {
                isExiting = false;
                exitTimer = 0f;
                if (showDebug) Debug.Log("[PortArea] Cooldown terminado");
                CheckBoatInZones();
            }
        }

        #endregion

        #region Gizmos & GUI

        private void OnDrawGizmos()
        {
            Gizmos.color = outerGizmoColor; DrawWireCircle(transform.position, outerRadius, 32);
            Gizmos.color = innerGizmoColor; DrawWireCircle(transform.position, innerRadius, 32);
            if (dockingPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(dockingPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, dockingPoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green; DrawWireCircle(transform.position, outerRadius, 64);
            Gizmos.color = Color.red; DrawWireCircle(transform.position, innerRadius, 64);
#if UNITY_EDITOR
            if (dockingPoint != null)
                UnityEditor.Handles.Label(dockingPoint.position + Vector3.up, "Punto de amarre");
#endif
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float step = 360f / segments;
            Vector3 prev = center + new Vector3(radius, 0, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i * Mathf.Deg2Rad;
                Vector3 curr = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prev, curr);
                prev = curr;
            }
        }

        private void OnGUI()
        {
            if (!showDebug) return;
            GUIStyle s = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            s.normal.textColor = Color.yellow;
            Rect r = new Rect((Screen.width - 500) / 2, Screen.height - 100, 500, 40);
            GUI.color = new Color(0, 0, 0, 0.7f); GUI.Box(r, ""); GUI.color = Color.white;
            string msg = isAutoPiloting ? autoPilotMessage : (boatInOuterZone && !isInShop && !isExiting) ? dockingMessage : "";
            if (msg != "") GUI.Label(r, msg, s);
        }

        #endregion
    }
}