using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    public class PortArea : MonoBehaviour
    {
        // Sistema de atraque del barco con dos zonas: Detección y Docking. Zona Exterior: Muestra UI 
        // Auto-Pilot: El barco navega automáticamente al punto de atraque. Zona Interior: Se abre la tienda cuando llega al punto exacto

        [Header("Detection Zones")]
        [Tooltip("Radio de la zona exterior (detección)")]
        [SerializeField] private float outerRadius = 15f;

        [Tooltip("Radio de la zona interior (tienda)")]
        [SerializeField] private float innerRadius = 3f;

        [Header("Docking")]
        [Tooltip("Punto exacto donde debe llegar el barco")]
        [SerializeField] private Transform dockingPoint;

        [Tooltip("Velocidad del auto-pilot")]
        [SerializeField] private float dockingSpeed = 2f;

        [Tooltip("Distancia mínima para considerar que llegó")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        [Tooltip("Cooldown para evitar re-captura inmediata (segundos)")]
        [SerializeField] private float exitCooldown = 2f;

        [Header("References")]
        [Tooltip("Tag del barco")]
        [SerializeField] private string targetTag = "Boat";

        [SerializeField] private GameObject shopUIPanel;

        [Header("UI Messages")]
        [SerializeField] private string dockingMessage = "Press 'E' to Dock";
        [SerializeField] private string autoPilotMessage = "Auto-Pilot Active...";

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color outerGizmoColor = new Color(0f, 1f, 0.5f, 0.2f);
        [SerializeField] private Color innerGizmoColor = new Color(1f, 0.5f, 0f, 0.3f);

        
        private bool boatInOuterZone = false;
        private bool isAutoPiloting = false;
        private bool isInShop = false;
        private bool isExiting = false;
        private float exitTimer = 0f;

        private GameObject detectedBoat;
        private Rigidbody boatRb;
        private BoatMovement boatMovement;

        
        private AbyssalReachControls controls;

        #region Unity ciclo de vida

        private void Awake()
        {
            controls = new AbyssalReachControls();            
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.Enable();

            // Asignar evento al botón de interacción (definido en Input System)
            // Asumiendo que has creado una acción (Interact) en el mapa BoatControls, de los input actions
            controls.BoatControls.Interact.performed += OnDockPressed;
        }

        private void OnDisable()
        {
            controls.BoatControls.Interact.performed -= OnDockPressed;
            controls.BoatControls.Disable();
            controls.Disable();
        }

        private void OnDockPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Solo procesar si estamos en zona exterior y no en auto-pilot
            if (boatInOuterZone && !isAutoPiloting && !isInShop)
            {
                StartAutoPilot();
            }
        }

        private void Update()
        {
            UpdateCooldown();
            CheckBoatInZones();

            if (isAutoPiloting)
            {
                UpdateAutoPilot();
            }
        }

        #endregion

        #region Zone Detection

        private void CheckBoatInZones()
        {
           
            GameObject boat = GameObject.FindGameObjectWithTag(targetTag);
            // Si no encontramos el barco, reseteamos estados y salimos
            if (boat == null)
            {
               
                if (boatInOuterZone || detectedBoat != null)
                {
                    if (showDebug)
                    {
                        Debug.Log("[PortArea] Barco no encontrado - Reseteando estado");
                    }
                }

                boatInOuterZone = false;
                detectedBoat = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, boat.transform.position);

            // Verificar zona exterior, y detectar cambios de estado para notificar eventos de entrada/salida
            if (!isExiting)
            {
                // Verificar zona exterior
                bool wasInOuterZone = boatInOuterZone;
                boatInOuterZone = distance <= outerRadius;

                if (boatInOuterZone && !wasInOuterZone)
                {
                    OnBoatEnterOuterZone(boat);
                }
                else if (!boatInOuterZone && wasInOuterZone)
                {
                    OnBoatExitOuterZone();
                }
            }
            else
            {
                // Durante el cooldown, seguir rastreando la distancia pero no cambiar estado
                if (showDebug && Time.frameCount % 60 == 0) // Log cada 60 frames
                {
                    Debug.Log("[PortArea] Cooldown activo - Distancia: " + distance.ToString("F1") + "m | Tiempo restante: " + exitTimer.ToString("F1") + "s");
                }
            }
        }

        private void OnBoatEnterOuterZone(GameObject boat)
        {
            detectedBoat = boat;
            boatRb = boat.GetComponent<Rigidbody>();
            boatMovement = boat.GetComponent<BoatMovement>();

            if (showDebug)
            {
                Debug.Log("[PortArea] Barco en zona de detección");
            }

            // Notificar al GameController 
            if (GameController.Instance != null)
            {
                GameController.Instance.EnterPort();
            }
        }

        private void OnBoatExitOuterZone()
        {
            detectedBoat = null;

            if (showDebug)
            {
                Debug.Log("[PortArea] Barco salió de zona de detección");
            }
            // Si el barco sale de la zona exterior, también consideramos que salió de la tienda, si estaba dentro
            if (!isInShop)
            {
                detectedBoat = null;
            }
            // Si estaba en auto-pilot, cancelar
            if (isAutoPiloting)
            {
                CancelAutoPilot();
            }

            // Notificar al GameController
            if (GameController.Instance != null)
            {
                GameController.Instance.ExitPort();
            }
        }

        #endregion

        #region Auto-Pilot

        private void StartAutoPilot()
        {
            if (dockingPoint == null || detectedBoat == null)
            {
                return;
            }

            isAutoPiloting = true;

            // Desactivar control manual del barco
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(false);
            }

            if (showDebug)
            {
                Debug.Log("[PortArea] Auto-Pilot activado");
            }
        }

        private void UpdateAutoPilot()
        {
            if (detectedBoat == null || dockingPoint == null)
            {
                CancelAutoPilot();
                return;
            }

            // Calcular dirección hacia el punto de atraque
            Vector3 targetPos = dockingPoint.position;
            Vector3 currentPos = detectedBoat.transform.position;
            Vector3 direction = (targetPos - currentPos).normalized;

            // Calcular distancia
            float distance = Vector3.Distance(currentPos, targetPos);

            // Verificar si llegamos
            if (distance <= arrivalThreshold)
            {
                ArriveAtDock();
                return;
            }

            // Mover el barco hacia el punto
            // Reducir velocidad gradualmente según la distancia
            float speedMultiplier = Mathf.Clamp01(distance / 5f); // Frena cuando está a menos de 5m
            float currentSpeed = dockingSpeed * speedMultiplier;

            if (boatRb != null)
            {
                Vector3 movement = direction * currentSpeed * Time.deltaTime;
                boatRb.MovePosition(boatRb.position + movement);

                // Asegurar que no rota
                boatRb.angularVelocity = Vector3.zero;
            }
        }

        private void ArriveAtDock()
        {
            isAutoPiloting = false;

            // Detener el barco completamente
            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
                boatRb.angularVelocity = Vector3.zero;
                boatRb.isKinematic = true;
            }

            if (boatMovement != null)
            {
                boatMovement.Stop();
            }

            // Abrir tienda
            OpenShop();

            if (showDebug)
            {
                Debug.Log("[PortArea] Barco atracado - Tienda abierta");
            }
        }

        private void CancelAutoPilot()
        {
            isAutoPiloting = false;

            // Reactivar control manual
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            if (showDebug)
            {
                Debug.Log("[PortArea] Auto-Pilot cancelado");
            }
        }

        #endregion

        #region Shop Control

        private void OpenShop()
        {
            if (shopUIPanel == null)
            {
                Debug.LogWarning("[PortArea] Shop UI Panel no asignado");
                return;
            }

            isInShop = true;
            shopUIPanel.SetActive(true);

            // Desactivar controles del barco, activar controles de UI
            controls.BoatControls.Disable();
            if (showDebug)
            {
                Debug.Log("[PortArea] Tienda abierta");
            }
        }

        public void CloseShop()
        {
            if (shopUIPanel == null)
            {
                return;
            }

            isInShop = false;
            shopUIPanel.SetActive(false);

            // Reactivar controles del barco
            controls.BoatControls.Enable();

            // Iniciar cooldown de salida
            StartExitCooldown();

            // Reactivar control manual
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            if (showDebug)
            {
                Debug.Log("[PortArea] Tienda cerrada - Cooldown de salida activo");
            }
        }

        #endregion

        #region Exit Cooldown

        private void StartExitCooldown()
        {
            isExiting = true;
            exitTimer = exitCooldown;
            boatInOuterZone = false; // Evitar que el jugador pueda reactivar la zona inmediatamente
        }

        private void UpdateCooldown()
        {
            if (isExiting)
            {
                exitTimer = exitTimer - Time.deltaTime;

                if (exitTimer <= 0f)
                {
                    isExiting = false;
                    exitTimer = 0f;

                    
                    Debug.Log("[PortArea] Cooldown terminado - Zona activa de nuevo");
                    
                    CheckBoatInZones();// Re-evaluar si el barco está en la zona al terminar el cooldown
                }
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            // Zona exterior (detección)
            Gizmos.color = outerGizmoColor;
            DrawWireCircle(transform.position, outerRadius, 32);

            // Zona interior (tienda)
            Gizmos.color = innerGizmoColor;
            DrawWireCircle(transform.position, innerRadius, 32);

            // Punto de atraque
            if (dockingPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(dockingPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, dockingPoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            DrawWireCircle(transform.position, outerRadius, 64);

            Gizmos.color = Color.red;
            DrawWireCircle(transform.position, innerRadius, 64);

            if (dockingPoint != null)
            {
                UnityEditor.Handles.Label(dockingPoint.position + Vector3.up,"Punto de amarre");
            }
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle) * radius,Mathf.Sin(angle) * radius,0);

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            float width = 500;
            float height = 40;
            Rect rect = new Rect((Screen.width - width) / 2,Screen.height - 100,width,height);

            // Fondo semitransparente
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, "");
            GUI.color = Color.white;

            // Mostrar mensaje según estado
            string message = "";

            if (isAutoPiloting)
            {
                message = autoPilotMessage;
            }
            else if (boatInOuterZone && !isInShop && !isExiting)
            {
                message = dockingMessage;
            }

            if (message != "")
            {
                GUI.Label(rect, message, style);
            }
        }

        #endregion
    }
}