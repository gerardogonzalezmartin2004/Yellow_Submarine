using UnityEngine;
using AbyssalReach.Gameplay;
using UnityEngine.UI;

namespace AbyssalReach.Core
{
    public class GameController : MonoBehaviour
    {
        private static GameController instance;
        public static GameController Instance => instance;

        [Header("References")]
        [SerializeField] private GameObject boat;
        [SerializeField] private GameObject diver;
        [SerializeField] private GameObject tetherSystem;
        [SerializeField] private TetherSystem tether;
        [SerializeField] private GameObject boatCamera;
        [SerializeField] private GameObject diverCamera;
        [SerializeField] private GameObject ropeObject;
        [SerializeField] private GameObject bagObject;
        [SerializeField] private BagFillVisualizer bagVisualizer;

        [Header("Inventory")]
        [SerializeField] private InventoryController inventoryController;

        [Header("Timer")]
        [SerializeField] private float oxygenTimer;
        [SerializeField] private float maxTimer;
        [SerializeField] private Slider oxygenSlider;
        private bool stopTimer;
        private bool emergencyAscent = false;

        [Header("Lantern")]
        [SerializeField] private float lanternTimer;
        [SerializeField] private float lanternMax;
        [SerializeField] private GameObject lightCone;

        [Header("Anchor Points")]
        [SerializeField] private Transform boatAnchor;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Sailing;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private Rigidbody boatRb;
        private BoatMovement boatMovement;
        private DiverMovement diverMovement;
        private AbyssalReachControls controls;
        private bool isDiving = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            controls = new AbyssalReachControls();

            // Global nunca se apaga — contiene ToggleInventory que debe
            // funcionar en cualquier estado del juego.
            controls.Global.Enable();

            if (boat != null)
            {
                boatRb = boat.GetComponent<Rigidbody>();
                boatMovement = boat.GetComponent<BoatMovement>();
            }
            if (diver != null)
                diverMovement = diver.GetComponent<DiverMovement>();

            stopTimer = false;
            oxygenSlider.maxValue = maxTimer;
            oxygenSlider.value = oxygenTimer;
            oxygenTimer = maxTimer;
            tether = tetherSystem.GetComponent<TetherSystem>();
        }

        private void OnEnable()
        {
            controls.Enable();
            // Re-aseguramos Global tras Enable general.
            controls.Global.Enable();
            controls.BoatControls.StartDive.performed += OnStartDivePressed;
            SetInputToGameplay();
        }

        private void OnDisable()
        {
            controls.BoatControls.StartDive.performed -= OnStartDivePressed;
            controls.Global.Disable();
            controls.Disable();
        }

        private void Start()
        {
            SetSailingMode();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
                ToggleDiving();

            if (currentState == GameState.Diving && !stopTimer)
            {
                oxygenTimer -= Time.deltaTime;
                oxygenSlider.value = oxygenTimer;

                if (oxygenTimer <= 0f)
                {
                    diverMovement?.EnterEmergencyAscent();
                    stopTimer = true;
                    emergencyAscent = true;
                    Debug.Log("[GameController] Oxígeno agotado");
                }
            }

            if (diverMovement != null && diverMovement.emergencyAscent && tether != null)
                tether.ReelInRope(Time.deltaTime * 5f);
        }

        #endregion

        #region Input

        private void OnStartDivePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (currentState == GameState.Sailing || currentState == GameState.Diving)
                ToggleDiving();
        }

        public void SetInputToUI()
        {
            controls.BoatControls.Disable();
            controls.DiverControls.Disable();
            controls.UI.Enable();
            if (showDebug) Debug.Log("[GameController] Input → UI");
        }

        public void SetInputToGameplay()
        {
            controls.UI.Disable();
            if (isDiving) controls.DiverControls.Enable();
            else controls.BoatControls.Enable();
            if (showDebug) Debug.Log("[GameController] Input → Gameplay");
        }

        #endregion

        #region Game States

        private void ToggleDiving()
        {
            if (isDiving)
            {
                SetSailingMode();
                stopTimer = false;
                diverMovement.ExitEmergencyAscent();
                tether.maxLength = 10f;
                diverMovement.emergencyAscent = false;
            }
            else
            {
                SetDivingMode();
                diverMovement.ExitEmergencyAscent();
                diverMovement.emergencyAscent = false;
            }
        }

        // Alterna entre Sailing e Inventory.
        // Solo funciona si estamos en Sailing o ya en Inventory.
        // El callback del Global/ToggleInventory llama a este método.
        public void ToggleInventory()
        {
            if (currentState == GameState.Sailing)
            {
                currentState = GameState.Inventory;

                // Paramos el barco mientras el jugador gestiona el inventario.
                boatMovement?.SetMovementActive(false);

                // Cambiamos a inputs de UI para que el ratón funcione en el Canvas.
                controls.BoatControls.Disable();
                controls.UI.Enable();

                // SetInventoryVisible(true) también dispara TransferDiverLoot internamente.
                inventoryController?.SetInventoryVisible(true);

                if (showDebug) Debug.Log("[GameController] Estado → Inventory");
            }
            else if (currentState == GameState.Inventory)
            {
                currentState = GameState.Sailing;

                boatMovement?.SetMovementActive(true);

                controls.UI.Disable();
                controls.BoatControls.Enable();

                inventoryController?.SetInventoryVisible(false);

                if (showDebug) Debug.Log("[GameController] Estado → Sailing");
            }
            // Si estamos buceando, en puerto, o pausados, ToggleInventory no hace nada.
        }

        public void SetSailingMode()
        {
            isDiving = false;
            currentState = GameState.Sailing;
            boatCamera.SetActive(true);
            diverCamera.SetActive(false);
            ropeObject.SetActive(false);
            bagObject.SetActive(false);

            if (boat != null) boat.SetActive(true);
            boatMovement?.SetMovementActive(true);
            if (boatRb != null) boatRb.isKinematic = false;
            if (diver != null) diver.SetActive(false);
            if (tetherSystem != null) tetherSystem.SetActive(false);

            controls.BoatControls.Enable();
            controls.DiverControls.Disable();
            controls.UI.Disable();

            oxygenTimer = maxTimer;
            oxygenSlider.value = maxTimer;

            if (showDebug) Debug.Log("[GameController] Modo Navegación Activado");
        }

        public void SetDivingMode()
        {
            isDiving = true;
            stopTimer = false;
            currentState = GameState.Diving;
            boatCamera.SetActive(false);
            diverCamera.SetActive(true);
            ropeObject.SetActive(true);
            bagObject.SetActive(true);

            boatMovement?.Stop();
            boatMovement?.SetMovementActive(false);

            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
                boatRb.angularVelocity = Vector3.zero;
                boatRb.isKinematic = true;
            }

            if (diver != null && boat != null)
            {
                Vector3 spawnPos = boatAnchor != null ? boatAnchor.position : boat.transform.position;
                spawnPos.y -= 2f;
                diver.SetActive(true);
                diverMovement?.SetPosition(spawnPos);
                diverMovement?.SetBoatReference(boat.transform);
            }

            if (tetherSystem != null) tether.ResetTetherToMax();

            controls.BoatControls.Disable();
            controls.DiverControls.Enable();
            controls.UI.Disable();

            if (showDebug) Debug.Log("[GameController] Modo Buceo Activado");
        }

        public void EnterPort()
        {
            if (currentState != GameState.Sailing) return;
            currentState = GameState.InPort;
            boatMovement?.Stop();
            boatMovement?.SetMovementActive(false);
            if (boatRb != null) { boatRb.linearVelocity = Vector3.zero; boatRb.isKinematic = true; }
        }

        public void ExitPort()
        {
            if (currentState == GameState.InPort) SetSailingMode();
        }

        public void StartDive() { if (currentState == GameState.Sailing) SetDivingMode(); }

        public void EndDive()
        {
            if (currentState != GameState.Diving) return;
            SetSailingMode();
            emergencyAscent = false;
            stopTimer = false;
            diverMovement?.ExitEmergencyAscent();
            if (diverMovement != null) diverMovement.emergencyAscent = false;
            tether?.ResetTetherToMax();
        }

        public enum GameState { Sailing, Diving, InPort, Paused, Inventory }

        #endregion

        #region Getters

        public GameState GetCurrentState() => currentState;
        public bool IsEmergencyAscent() => emergencyAscent;
        public bool IsDiving() => isDiving;
        public AbyssalReachControls GetControls() => controls;

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!showDebug) return;
            GUIStyle bold = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold };
            bold.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, 10, 300, 25), "STATE: " + currentState, bold);

            GUIStyle normal = new GUIStyle { fontSize = 14 };
            normal.normal.textColor = Color.white;
            if (boatRb != null)
                GUI.Label(new Rect(10, 55, 300, 20), "Boat Vel: " + boatRb.linearVelocity.magnitude.ToString("F2"), normal);

            normal.normal.textColor = Color.cyan;
            GUI.Label(new Rect(10, 100, 400, 20), "F5: Toggle Sailing/Diving | I: Toggle Inventory", normal);
        }

        #endregion
    }
}