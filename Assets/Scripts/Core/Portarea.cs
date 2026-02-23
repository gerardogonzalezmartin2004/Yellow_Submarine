using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    public class PortArea : MonoBehaviour
    {
        // Sistema de atraque del barco con dos zonas: Detección y Docking. Zona Exterior: Muestra UI 
        // Auto-Pilot: El barco navega automáticamente al punto de atraque. Zona Interior: Se abre la tienda cuando llega al punto exacto

        private enum PortState
        {
            Idle,           // Barco lejos del puerto
            InGreenZone,    // En zona de detección, esperando input
            AutoPiloting,   // Navegando automáticamente al punto de atraque
            InShop,         // Tienda abierta, barco atracado
            Exiting,        // Saliendo automáticamente del puerto
            Cooldown        // Periodo de espera antes de poder re-entrar
        }

        [Header("Detection Zones")]
        [Tooltip("Radio de la zona verde (detección)")]
        [SerializeField] private float greenZoneRadius = 15f;

        [Tooltip("Radio de la zona amarilla (límite físico)")]
        [SerializeField] private float yellowZoneRadius = 8f;

        [Header("Docking")]
        [Tooltip("Punto exacto donde debe llegar el barco")]
        [SerializeField] private Transform dockingPoint;

        [Tooltip("Velocidad del auto-pilot")]
        [SerializeField] private float dockingSpeed = 2f;

        [Tooltip("Distancia mínima para considerar que llegó")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        [Header("Exit Behavior")]
        [Tooltip("Distancia que retrocede al salir de la tienda (metros)")]
        [SerializeField] private float exitDistance = 10f;

        [Tooltip("Velocidad de retroceso al salir")]
        [SerializeField] private float exitSpeed = 3f;

        [Tooltip("Cooldown antes de poder re-entrar (segundos)")]
        [SerializeField] private float reEntryCooldown = 2f;

        [Header("References")]
        [Tooltip("Tag del barco")]
        [SerializeField] private string targetTag = "Boat";

        [SerializeField] private GameObject shopUIPanel;

        [Header("Yellow Zone Blocker")]
        [Tooltip("Collider físico que bloquea la zona amarilla")]
        [SerializeField] private Collider yellowZoneBlocker;

        [Header("UI Messages")]
        [SerializeField] private string dockingMessage = "Press 'E' to Dock";
        [SerializeField] private string autoPilotMessage = "Auto-Pilot Active...";

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color greenGizmoColor = new Color(0f, 1f, 0.5f, 0.2f);
        [SerializeField] private Color yellowGizmoColor = new Color(1f, 0.8f, 0f, 0.3f);

       
        private PortState currentState = PortState.Idle;

        
        private GameObject detectedBoat;
        private Rigidbody boatRb;
        private BoatMovement boatMovement;

        // Variables de auto-pilot de entrada
        private Vector3 autoPilotStartPos;
        private float autoPilotProgress = 0f;

        // Variables de auto-pilot de salida
        private Vector3 exitStartPos;
        private Vector3 exitTargetPos;
        private float exitProgress = 0f;

        // Cooldown
        private float cooldownTimer = 0f;

        // Input System
        private AbyssalReachControls controls;

        #region Unity ciclo de vida

        private void Awake()
        {
            controls = new AbyssalReachControls();
            // Validar dockingPoint
            if (dockingPoint == null)
            {
                Debug.LogError("[PortArea] Docking Point no asignado");
            }

            // Configurar el bloqueador de zona amarilla
            if (yellowZoneBlocker != null)
            {
                yellowZoneBlocker.enabled = true; // Empieza activo (bloqueando)
            }
            else
            {
                Debug.LogWarning("[PortArea] Yellow Zone Blocker no asignado no habrá muro físico");
            }
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


        private void Update()
        {
            UpdateStateMachine();
        }
        #endregion

        #region State Machine

        private void UpdateStateMachine()
        {
            switch (currentState)
            {
                case PortState.Idle:
                    UpdateIdle();
                    break;

                case PortState.InGreenZone:
                    UpdateInGreenZone();
                    break;

                case PortState.AutoPiloting:
                    UpdateAutoPiloting();
                    break;

                case PortState.InShop:
                    UpdateInShop();
                    break;

                case PortState.Exiting:
                    UpdateExiting();
                    break;

                case PortState.Cooldown:
                    UpdateCooldown();
                    break;
            }
        }

        private void UpdateIdle()
        {
            // Buscar el barco constantemente
            GameObject boat = GameObject.FindGameObjectWithTag(targetTag);

            if (boat == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, boat.transform.position);

            // ¿Entró en la zona verde?
            if (distance <= greenZoneRadius)
            {
                EnterGreenZone(boat);
            }
        }

        private void UpdateInGreenZone()
        {
            if (detectedBoat == null)
            {
                ChangeState(PortState.Idle);
                return;
            }

            float distance = Vector3.Distance(transform.position, detectedBoat.transform.position);

            // ¿Salió de la zona verde?
            if (distance > greenZoneRadius)
            {
                ExitGreenZone();
            }

            // El jugador navega libremente aquí, esperando que pulse 'E'
        }

        private void UpdateAutoPiloting()
        {
            if (detectedBoat == null || dockingPoint == null)
            {
                CancelAutoPilot();
                return;
            }

            // Mover el barco hacia el punto de atraque
            Vector3 targetPos = dockingPoint.position;
            Vector3 currentPos = detectedBoat.transform.position;

            // Calcular dirección
            Vector3 direction = (targetPos - currentPos).normalized;
            float distance = Vector3.Distance(currentPos, targetPos);

            // ¿Llegamos?
            if (distance <= arrivalThreshold)
            {
                ArriveAtDock();
                return;
            }

            // Reducir velocidad gradualmente según la distancia
            float speedMultiplier = Mathf.Clamp01(distance / 5f);
            float currentSpeed = dockingSpeed * speedMultiplier;

            // Mover el Rigidbody
            if (boatRb != null)
            {
                Vector3 movement = direction * currentSpeed * Time.deltaTime;
                boatRb.MovePosition(boatRb.position + movement);
                boatRb.angularVelocity = Vector3.zero;
            }
        }

        private void UpdateInShop()
        {
            // El barco está quieto, el jugador navega en la UI
            // Esperar a que cierre la tienda (gestionado por ShopUI)
        }

        private void UpdateExiting()
        {
            if (detectedBoat == null)
            {
                ChangeState(PortState.Idle);
                return;
            }

            // Mover el barco hacia el punto de salida
            exitProgress = exitProgress + (exitSpeed * Time.deltaTime / exitDistance);

            if (exitProgress >= 1f)
            {
                // Salida completada
                FinishExit();
                return;
            }

            // Interpolar posición
            Vector3 newPos = Vector3.Lerp(exitStartPos, exitTargetPos, exitProgress);

            if (boatRb != null)
            {
                boatRb.MovePosition(newPos);
                boatRb.angularVelocity = Vector3.zero;
            }
        }

        private void UpdateCooldown()
        {
            cooldownTimer = cooldownTimer - Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                ChangeState(PortState.Idle);
            }

            // Durante el cooldown, verificar si el barco se aleja
            if (detectedBoat != null)
            {
                float distance = Vector3.Distance(transform.position, detectedBoat.transform.position);

                if (distance > greenZoneRadius)
                {
                    // Si se aleja, cancelar cooldown inmediatamente
                    ChangeState(PortState.Idle);
                }
            }
        }

        #endregion

        #region State Transitions

        private void EnterGreenZone(GameObject boat)
        {
            detectedBoat = boat;
            boatRb = boat.GetComponent<Rigidbody>();
            boatMovement = boat.GetComponent<BoatMovement>();

            ChangeState(PortState.InGreenZone);

            if (showDebug)
            {
                Debug.Log("[PortArea] Barco entró en Zona Verde");
            }

            // Notificar al GameController
            if (GameController.Instance != null)
            {
                GameController.Instance.EnterPort();
            }
        }

        private void ExitGreenZone()
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Barco salió de Zona Verde");
            }

            ChangeState(PortState.Idle);

            // Notificar al GameController
            if (GameController.Instance != null)
            {
                GameController.Instance.ExitPort();
            }

            detectedBoat = null;
        }

        private void StartAutoPilot()
        {
            if (dockingPoint == null || detectedBoat == null)
            {
                return;
            }

            ChangeState(PortState.AutoPiloting);

            // Desactivar el bloqueador amarillo (permitir entrar)
            if (yellowZoneBlocker != null)
            {
                yellowZoneBlocker.enabled = false;
            }

            // Desactivar control manual del barco
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(false);
            }

            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
            }

            autoPilotStartPos = detectedBoat.transform.position;
            autoPilotProgress = 0f;

            if (showDebug)
            {
                Debug.Log("[PortArea] Auto-Pilot activado");
            }
        }

        private void CancelAutoPilot()
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Auto-Pilot cancelado");
            }

            // Reactivar el bloqueador
            if (yellowZoneBlocker != null)
            {
                yellowZoneBlocker.enabled = true;
            }

            // Reactivar control manual
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            ChangeState(PortState.InGreenZone);
        }

        private void ArriveAtDock()
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Barco atracado - Abriendo tienda");
            }

            // Congelar barco completamente
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

            ChangeState(PortState.InShop);
        }

        private void StartExit()
        {
            if (detectedBoat == null)
            {
                return;
            }

            ChangeState(PortState.Exiting);

            // Calcular posición de salida (retroceder en línea recta)
            exitStartPos = detectedBoat.transform.position;

            Vector3 directionFromPort = (exitStartPos - transform.position).normalized;
            exitTargetPos = transform.position + (directionFromPort * greenZoneRadius * 0.8f);

            exitProgress = 0f;

            // Asegurar que el Rigidbody no es kinematic
            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            if (showDebug)
            {
                Debug.Log("[PortArea] Iniciando salida automática");
            }
        }

        private void FinishExit()
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Salida completada - Iniciando cooldown");
            }

            // Reactivar el bloqueador amarillo
            if (yellowZoneBlocker != null)
            {
                yellowZoneBlocker.enabled = true;
            }

            // Devolver control al jugador
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            // Iniciar cooldown
            cooldownTimer = reEntryCooldown;
            ChangeState(PortState.Cooldown);
        }

        #endregion

        #region Input Callbacks

        private void OnDockPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Solo procesar en zona verde
            if (currentState == PortState.InGreenZone)
            {
                StartAutoPilot();
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

            shopUIPanel.SetActive(true);

            // CRÍTICO: Cambiar Input Map a UI
            if (GameController.Instance != null)
            {
                GameController.Instance.SetInputToUI();
            }

            // Desactivar controles del barco
            controls.BoatControls.Disable();

            if (showDebug)
            {
                Debug.Log("[PortArea] Tienda abierta - Input cambiado a UI");
            }
        }

        public void CloseShop()
        {
            if (shopUIPanel == null)
            {
                return;
            }

            shopUIPanel.SetActive(false);

            // CRÍTICO: Restaurar Input Map a Gameplay
            if (GameController.Instance != null)
            {
                GameController.Instance.SetInputToGameplay();
            }

            // Reactivar controles del barco
            controls.BoatControls.Enable();

            if (showDebug)
            {
                Debug.Log("[PortArea] Tienda cerrada - Iniciando salida");
            }

            // Iniciar salida automática
            StartExit();
        }

        #endregion

        #region Helper Methods

        private void ChangeState(PortState newState)
        {
            if (currentState == newState)
            {
                return;
            }

            if (showDebug)
            {
                Debug.Log("[PortArea] Estado: " + currentState + " -> " + newState);
            }

            currentState = newState;
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            // Zona verde (detección)
            Gizmos.color = greenGizmoColor;
            DrawWireCircle(transform.position, greenZoneRadius, 32);

            // Zona amarilla (límite físico)
            Gizmos.color = yellowGizmoColor;
            DrawWireCircle(transform.position, yellowZoneRadius, 32);

            // Punto de atraque
            if (dockingPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(dockingPoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, dockingPoint.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            DrawWireCircle(transform.position, greenZoneRadius, 64);

            Gizmos.color = Color.yellow;
            DrawWireCircle(transform.position, yellowZoneRadius, 64);

            if (dockingPoint != null)
            {
                UnityEditor.Handles.Label(dockingPoint.position + Vector3.up, "Punto de Atraque");
            }
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i = i + 1)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

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
            Rect rect = new Rect((Screen.width - width) / 2, Screen.height - 100, width, height);

            // Fondo semitransparente
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, "");
            GUI.color = Color.white;

            // Mostrar mensaje según estado
            string message = "";

            if (currentState == PortState.InGreenZone)
            {
                message = dockingMessage;
            }
            else if (currentState == PortState.AutoPiloting)
            {
                message = autoPilotMessage;
            }
            else if (currentState == PortState.Exiting)
            {
                message = "Dejando el puerto...";
            }
            else if (currentState == PortState.Cooldown)
            {
                message = "Cooldown: " + cooldownTimer.ToString("F1") + "s";
            }

            if (message != "")
            {
                GUI.Label(rect, message, style);
            }

            // Info de estado en esquina
            style.fontSize = 12;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.cyan;

            GUI.Label(new Rect(10, 570, 300, 20), "[PortArea] Estado: " + currentState, style);
        }

        #endregion
    }
}