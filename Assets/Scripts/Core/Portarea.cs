using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    // Sistema de puerto con zona de detección y auto-pilot.

    public class PortArea : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Detection Zones")]
        [Tooltip("Radio de la zona de detección (muestra UI)")]
        [SerializeField] private float detectionRadius = 15f;

        [Tooltip("Distancia mínima para considerar que llegó al dock")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        [Header("Docking")]
        [Tooltip("Punto exacto donde debe llegar el barco")]
        [SerializeField] private Transform dockingPoint;

        [Tooltip("Velocidad del auto-pilot")]
        [SerializeField] private float dockingSpeed = 3f;

        [Tooltip("Distancia para empezar a frenar")]
        [SerializeField] private float brakingDistance = 5f;

        [Header("References")]
        [Tooltip("Tag del barco")]
        [SerializeField] private string boatTag = "Boat";

        [Tooltip("Panel UI de la tienda")]
        [SerializeField] private GameObject shopUIPanel;

        [Header("Cooldown")]
        [Tooltip("Tiempo antes de poder re-atracar (segundos)")]
        [SerializeField] private float redockCooldown = 3f;

        [Header("UI Messages")]
        [SerializeField] private string dockPrompt = "Press 'E' to Dock";
        [SerializeField] private string autoPilotMessage = "Auto-Pilot Active...";
        [SerializeField] private string shopOpenMessage = "Press 'ESC' to Exit Shop";

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private Color detectionGizmoColor = new Color(0f, 1f, 0.5f, 0.3f);
        [SerializeField] private Color dockingGizmoColor = new Color(1f, 0.5f, 0f, 0.8f);

        #endregion

        #region Private Fields

        private PortState currentState = PortState.Idle;
        private GameObject boatObject;
        private Rigidbody boatRigidbody;
        private BoatMovement boatMovement;
        private AbyssalReachControls controls;
        private float cooldownTimer = 0f;
        private bool boatInDetectionZone = false;

        #endregion

        #region Enums

        private enum PortState
        {
            Idle,              
            BoatDetected,      
            AutoPiloting,      
            ShopOpen,         
            Cooldown         
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            controls = new AbyssalReachControls();
            ValidateReferences();
        }

        private void OnEnable()
        {
            EnableControls();
        }

        private void OnDisable()
        {
            DisableControls();
        }

        private void Update()
        {
            UpdateCooldownTimer();
            DetectBoat();
            UpdateStateMachine();
        }

        #endregion

        #region Input Management

        private void EnableControls()
        {
            controls.Enable();
            controls.BoatControls.Enable();
            controls.BoatControls.Interact.performed += OnInteractPressed;
            LogDebug("Controles de puerto habilitados");
        }

        private void DisableControls()
        {
            controls.BoatControls.Interact.performed -= OnInteractPressed;
            controls.BoatControls.Disable();
            controls.Disable();
            LogDebug("Controles de puerto deshabilitados");
        }

        private void OnInteractPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (currentState == PortState.BoatDetected)
            {
                StartDocking();
            }
        }

        #endregion

        #region Boat Detection

        private void DetectBoat()
        {
            GameObject boat = GameObject.FindGameObjectWithTag(boatTag);

            if (boat == null)
            {
                if (boatInDetectionZone)
                {
                    OnBoatExitZone();
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, boat.transform.position);
            bool inZone = distance <= detectionRadius;

            if (inZone && !boatInDetectionZone)
            {
                OnBoatEnterZone(boat);
            }
            else if (!inZone && boatInDetectionZone)
            {
                OnBoatExitZone();
            }
        }

        private void OnBoatEnterZone(GameObject boat)
        {
            boatInDetectionZone = true;
            boatObject = boat;
            boatRigidbody = boat.GetComponent<Rigidbody>();
            boatMovement = boat.GetComponent<BoatMovement>();

            if (currentState == PortState.Idle)
            {
                ChangeState(PortState.BoatDetected);
            }

            // Notificamos al GameController, pero el barco sigue moviendose
            if (GameController.Instance != null)
            {
                GameController.Instance.EnterPort();
            }

            LogDebug($"Barco detectado en zona - Estado: {currentState}");
        }

        private void OnBoatExitZone()
        {
            boatInDetectionZone = false;

            if (currentState == PortState.AutoPiloting)
            {
                CancelDocking();
            }

            if (currentState == PortState.BoatDetected)
            {
                ChangeState(PortState.Idle);
            }

            if (GameController.Instance != null)
            {
                GameController.Instance.ExitPort();
            }

            LogDebug("Barco salió de zona");
        }

        #endregion

        #region State Machine

        private void ChangeState(PortState newState)
        {
            if (currentState == newState) return;
            LogDebug($"Cambio de estado: {currentState} → {newState}");
            currentState = newState;
        }

        private void UpdateStateMachine()
        {
            switch (currentState)
            {
                case PortState.Idle:
                    break;
                case PortState.BoatDetected:
                    break;
                case PortState.AutoPiloting:
                    UpdateAutoPilot();
                    break;
                case PortState.ShopOpen:
                    break;
                case PortState.Cooldown:
                    break;
            }
        }

        #endregion

        #region Docking System

        private void StartDocking()
        {
            if (dockingPoint == null || boatObject == null)
            {
                Debug.LogWarning("[PortArea] No se puede iniciar docking - referencias faltantes");
                return;
            }

            ChangeState(PortState.AutoPiloting);

            //  detenemos el control del jugador porque asume el auto-pilot
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(false);
            }

            // Limpiamos cualquier rotación o fuerza residual antes de hacerlo cinemático
            if (boatRigidbody != null)
            {
                boatRigidbody.angularVelocity = Vector3.zero;
                boatRigidbody.linearVelocity = Vector3.zero;
                boatRigidbody.isKinematic = true;
            }

            if (GameController.Instance != null)
            {
                GameController.Instance.SetGameState(GameController.GameState.Docking);
            }

            LogDebug("Auto-Pilot iniciado - barco congelado y bajo control automático");
        }

        private void UpdateAutoPilot()
        {
            if (boatObject == null || dockingPoint == null)
            {
                CancelDocking();
                return;
            }

            Vector3 targetPos = dockingPoint.position;
            Vector3 currentPos = boatObject.transform.position;
            Vector3 direction = (targetPos - currentPos).normalized;
            float distance = Vector3.Distance(currentPos, targetPos);

            if (distance <= arrivalThreshold)
            {
                ArriveAtDock();
                return;
            }

            float speedMultiplier = Mathf.Clamp01(distance / brakingDistance);
            float currentSpeed = dockingSpeed * speedMultiplier;

            if (boatRigidbody != null)
            {
              
                Vector3 movement = direction * currentSpeed * Time.deltaTime;
                boatRigidbody.MovePosition(boatRigidbody.position + movement);

                // Mantenemos la velocidad angular a 0 por seguridad mientras es cinemático
                boatRigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void ArriveAtDock()
        {
            LogDebug("Barco atracado - Abriendo tienda");

            if (boatRigidbody != null)
            {
                boatRigidbody.linearVelocity = Vector3.zero;
                boatRigidbody.angularVelocity = Vector3.zero;
            }

            if (boatMovement != null)
            {
                boatMovement.Stop();
            }

            ChangeState(PortState.ShopOpen);
            OpenShop();
        }

        private void CancelDocking()
        {
            LogDebug("Auto-Pilot cancelado");

            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (boatRigidbody != null)
            {
                boatRigidbody.angularVelocity = Vector3.zero; // Limpieza preventiva
                boatRigidbody.isKinematic = false;
            }

            if (boatInDetectionZone)
            {
                ChangeState(PortState.BoatDetected);
            }
            else
            {
                ChangeState(PortState.Idle);
            }

            if (GameController.Instance != null)
            {
                GameController.Instance.SetGameState(GameController.GameState.Sailing);
            }
        }

        #endregion

        #region Shop Management

        private void OpenShop()
        {
            if (shopUIPanel == null)
            {
                Debug.LogWarning("[PortArea] Shop UI Panel no asignado");
                return;
            }

            shopUIPanel.SetActive(true);

            if (GameController.Instance != null)
            {
                GameController.Instance.SetGameState(GameController.GameState.InShop);
            }

            LogDebug("Tienda abierta");
        }

        public void CloseShop()
        {
            if (shopUIPanel == null) return;

            LogDebug("Cerrando tienda...");

            shopUIPanel.SetActive(false);

            if (boatRigidbody != null)
            {
                //  Limpiamos fuerzas rotacionales acumuladas antes de "despertar" el Rigidbody
                boatRigidbody.angularVelocity = Vector3.zero;
                boatRigidbody.isKinematic = false;
            }

            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (GameController.Instance != null)
            {
                GameController.Instance.SetGameState(GameController.GameState.Sailing);
            }

            StartCooldown();

            LogDebug("Tienda cerrada - Controles reactivados - Cooldown iniciado");
        }

        #endregion

        #region Cooldown System

        private void StartCooldown()
        {
            ChangeState(PortState.Cooldown);
            cooldownTimer = redockCooldown;
        }

        private void UpdateCooldownTimer()
        {
            if (currentState != PortState.Cooldown) return;

            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;

                if (boatInDetectionZone)
                {
                    ChangeState(PortState.BoatDetected);
                }
                else
                {
                    ChangeState(PortState.Idle);
                }

                LogDebug("Cooldown terminado");
            }
        }

        #endregion

        #region Utilities

        private bool IsInBoatMode()
        {
            if (GameController.Instance == null)
                return false;

            return GameController.Instance.GetCurrentState() == GameController.GameState.Sailing;
        }

        private void ValidateReferences()
        {
            if (dockingPoint == null)
                Debug.LogWarning("[PortArea] Docking Point no asignado");

            if (shopUIPanel == null)
                Debug.LogWarning("[PortArea] Shop UI Panel no asignado");
        }

        private void LogDebug(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PortArea] {message}");
            }
        }

        #region Debug Visualization



        // Movido a OnDrawGizmos para que SIEMPRE sea visible, no solo al seleccionarlo.

        private void OnDrawGizmos()

        {

            // Dibujamos el círculo del radio de detección

            Gizmos.color = detectionGizmoColor;

            DrawWireCircle(transform.position, detectionRadius, 32);



            if (dockingPoint != null)

            {

                Gizmos.color = dockingGizmoColor;

                Gizmos.DrawSphere(dockingPoint.position, 0.8f);

                Gizmos.DrawLine(transform.position, dockingPoint.position);

            }

        }



        private void OnDrawGizmosSelected()

        {

            // Resaltamos el punto de atraque al seleccionar

            if (dockingPoint != null)

            {

                Gizmos.color = Color.yellow;

                Gizmos.DrawWireSphere(dockingPoint.position, arrivalThreshold);



#if UNITY_EDITOR

                UnityEditor.Handles.Label(

                    dockingPoint.position + Vector3.up * 2f,

                    "Docking Point",

                    new GUIStyle()

                    {

                        normal = { textColor = Color.yellow },

                        fontSize = 14,

                        fontStyle = FontStyle.Bold

                    }

                );

#endif

            }

        }



        private void DrawWireCircle(Vector3 center, float radius, int segments)

        {

            float angleStep = 360f / segments;

            Vector3 previousPoint = center + new Vector3(radius, 0, 0);



            for (int i = 1; i <= segments; i++)

            {

                float angle = angleStep * i * Mathf.Deg2Rad;

                Vector3 currentPoint = center + new Vector3(

                    Mathf.Cos(angle) * radius,

                    0,

                    Mathf.Sin(angle) * radius

                );





                Gizmos.DrawLine(previousPoint, currentPoint);

                Gizmos.DrawLine(previousPoint + Vector3.up * 0.01f, currentPoint + Vector3.up * 0.01f);



                previousPoint = currentPoint;

            }

        }




        private void OnGUI()
        {

            if (!showDebugUI) return;



            GUIStyle style = new GUIStyle();

            style.fontSize = 24;

            style.normal.textColor = Color.white;

            style.fontStyle = FontStyle.Bold;

            style.alignment = TextAnchor.MiddleCenter;



            float width = 600;

            float height = 50;

            Rect rect = new Rect(

                (Screen.width - width) / 2,

                Screen.height - 120,

                width,

                height

            );



            GUI.color = new Color(0, 0, 0, 0.7f);

            GUI.Box(rect, "");

            GUI.color = Color.white;



            string message = "";



            switch (currentState)

            {

                case PortState.BoatDetected:

                    message = dockPrompt;

                    style.normal.textColor = Color.green;

                    break;



                case PortState.AutoPiloting:

                    message = autoPilotMessage;

                    style.normal.textColor = Color.yellow;

                    break;



                case PortState.ShopOpen:

                    message = shopOpenMessage;

                    style.normal.textColor = Color.cyan;

                    break;



                case PortState.Cooldown:

                    message = $"Cooldown: {cooldownTimer:F1}s";

                    style.normal.textColor = Color.red;

                    break;

            }



            if (!string.IsNullOrEmpty(message))

            {

                GUI.Label(rect, message, style);

            }



            if (showDebugLogs)

            {

                GUIStyle debugStyle = new GUIStyle();

                debugStyle.fontSize = 16;

                debugStyle.normal.textColor = Color.yellow;



                string debugText = $"PortState: {currentState}\n";

                debugText += $"Barco en zona: {boatInDetectionZone}\n";



                if (GameController.Instance != null)

                {

                    debugText += $"GameState: {GameController.Instance.GetCurrentState()}";

                }



                GUI.Label(new Rect(10, 120, 400, 80), debugText, debugStyle);

            }

        }

        #endregion



    }
    #endregion
} 