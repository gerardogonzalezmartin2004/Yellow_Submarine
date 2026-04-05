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

        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;

            if (showDebug)
            {
                Debug.Log($"[GameController] Estado cambiado: {currentState} → {newState}");
            }

            currentState = newState;

            switch (newState)
            {
                case GameState.Sailing:
                    controls.BoatControls.Enable();
                    controls.UI.Disable();
                    controls.DiverControls.Disable();
                    break;

                case GameState.Diving:
                    controls.BoatControls.Disable();
                    controls.UI.Disable();
                    controls.DiverControls.Enable();
                    break;

                case GameState.InPort:
                case GameState.Docking:
                    break;

                case GameState.InShop:
                    controls.BoatControls.Disable();
                    controls.DiverControls.Disable();
                    controls.UI.Enable();
                    break;

                case GameState.Inventory:
                    controls.BoatControls.Disable();
                    controls.DiverControls.Disable();
                    controls.UI.Enable();
                    break;

                case GameState.Paused:
                    controls.BoatControls.Disable();
                    controls.DiverControls.Disable();
                    controls.UI.Enable();
                    break;
            }
        }

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

        public void ToggleInventory()
        {
            if (currentState == GameState.Sailing)
            {
                SetGameState(GameState.Inventory);

                boatMovement?.SetMovementActive(false);

                controls.BoatControls.Disable();
                controls.UI.Enable();

                inventoryController?.SetInventoryVisible(true);
            }
            else if (currentState == GameState.Inventory)
            {
                SetGameState(GameState.Sailing);

                boatMovement?.SetMovementActive(true);

                controls.UI.Disable();
                controls.BoatControls.Enable();

                inventoryController?.SetInventoryVisible(false);
            }
        }

        public void SetSailingMode()
        {
            isDiving = false;
            SetGameState(GameState.Sailing);

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
            SetGameState(GameState.Diving);

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

       
        // Solo cambia el estado para que la UI muestre el mensaje.
        public void EnterPort()
        {
            if (currentState != GameState.Sailing) return;

            SetGameState(GameState.InPort);

            if (showDebug) Debug.Log("[GameController] Barco en zona de puerto - navegación libre");
        }

        public void ExitPort()
        {
            if (currentState == GameState.InPort)
                SetSailingMode();
        }

        public void StartDive()
        {
            if (currentState == GameState.Sailing)
                SetDivingMode();
        }

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

        public enum GameState
        {
            Sailing,      
            Diving,       
            InPort,       
            Docking,      
            InShop,      
            Inventory,    
            Paused        
        }

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
                GUI.Label(new Rect(10, 35, 300, 20), "Boat Vel: " + boatRb.linearVelocity.magnitude.ToString("F2"), normal);

            normal.normal.textColor = Color.cyan;
            string controlsState = "";
            if (controls.BoatControls.enabled) controlsState += "Boat ";
            if (controls.DiverControls.enabled) controlsState += "Diver ";
            if (controls.UI.enabled) controlsState += "UI ";
            GUI.Label(new Rect(10, 55, 400, 20), "Controls: " + controlsState, normal);

            normal.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 80, 400, 20), "F5: Toggle Sailing/Diving | I: Toggle Inventory", normal);
        }

        #endregion
    }
}