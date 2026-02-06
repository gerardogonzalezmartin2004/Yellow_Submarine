using UnityEngine;
using AbyssalReach.Gameplay; // Necesario para encontrar BoatMovement

namespace AbyssalReach.Core
{
    public class GameController : MonoBehaviour
    {
        public static GameController instance;
        public static GameController Instance
        {
            get
            {

                return instance;
            }
        }

        [Header("References")]
        [SerializeField] private GameObject boat;
        [SerializeField] private GameObject diver;
        [SerializeField] private GameObject tetherSystem;

        [Header("Anchor Points")]
        [SerializeField] private Transform boatAnchor;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Sailing;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private Rigidbody boatRb;
        private BoatMovement boatMovement;
        private DiverMovement diverMovement; // Referencia al script del buceador como el boat y los controles 
        private AbyssalReachControls controls;
        private bool isDiving = false;

        private void Awake()
        {
            // Configurar Singleton
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            controls = new AbyssalReachControls();

            if (boat != null)
            {
                boatRb = boat.GetComponent<Rigidbody>();
                boatMovement = boat.GetComponent<BoatMovement>();
            }
            if (diver != null)
            {
                diverMovement = diver.GetComponent<DiverMovement>();
            }
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.StartDive.performed += OnStartDivePressed; // Esto es como una llamada a la función ToggleDiving cada vez que se presiona el botón de bucear. Si ya estás buceando, te saca a navegar, y si estás navegando, te mete a bucear. Me lo dijo Serg

        }

        private void OnDisable()
        {
            controls.BoatControls.StartDive.performed -= OnStartDivePressed;
            controls.Disable();
        }
        private void OnStartDivePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            ToggleDiving();
        }

        private void Start()
        {
            // Empezar en modo navegación
            SetSailingMode();
        }
        private void Update()
        {

            if (showDebug && Input.GetKeyDown(KeyCode.F5))
            {
                ToggleDiving();
            }
        }

        private void ToggleDiving()
        {
            if (isDiving)
            {
                SetSailingMode();
            }
            else
            {
                SetDivingMode();
            }
        }

        private void SetSailingMode()
        {
            isDiving = false;
            currentState = GameState.Sailing;

            // Activar barco
            if (boat != null)
            {
                boat.SetActive(true);
            }

            // Desactivar buceador
            if (diver != null)
            {
                diver.SetActive(false);
            }

            // Desactivar cable
            if (tetherSystem != null)
            {
                tetherSystem.SetActive(false);
            }

            // IMPORTANTE: Activar movimiento del barco
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            // Rigidbody del barco normal (no kinematic)
            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            controls.BoatControls.Enable();
            controls.DiverControls.Disable();

            if (showDebug)
            {
                Debug.Log("[GameController] SAILING MODE");
            }
        }

        private void SetDivingMode()
        {
            isDiving = true;
            currentState = GameState.Diving;


            if (boatRb != null)
            {

                boatRb.linearVelocity = Vector3.zero;  // Frenar en seco
                boatRb.angularVelocity = Vector3.zero; // Frenar giros
                boatRb.isKinematic = true; // Hacerlo inmóvil ante golpes o física

            }
            if (boatMovement != null)
            {
                boatMovement.Stop();
                boatMovement.SetMovementActive(false);
            }

            // Posicionar buceador debajo del barco
            if (diver != null && boat != null)
            {
                Vector3 spawnPos = boat.transform.position;
                if (boatAnchor != null)
                {
                    spawnPos = boatAnchor.position;
                }
                spawnPos.y = spawnPos.y - 2f;

                diver.SetActive(true);

                if (diverMovement != null)
                {
                    diverMovement.SetPosition(spawnPos);
                    diverMovement.SetBoatReference(boat.transform);
                }
            }

            // Activar cable
            if (tetherSystem != null)
            {
                tetherSystem.SetActive(true);
            }

            //Y activamos al buceador 
            controls.BoatControls.Disable();
            controls.DiverControls.Enable();

            if (showDebug)
            {
                Debug.Log("[GameController] DIVING MODE - Barco anclado");
            }
        }

        public void EnterPort()
        {
            if (currentState == GameState.Sailing)
            {
                currentState = GameState.InPort;

                // Detener barco en el puerto
                if (boatMovement != null)
                {
                    boatMovement.Stop();
                    boatMovement.SetMovementActive(false);
                }

                if (boatRb != null)
                {
                    boatRb.linearVelocity = Vector3.zero;
                    boatRb.isKinematic = true;
                }

                if (showDebug)
                {
                    Debug.Log("[GameController] En el puerto");
                }
            }
        }

        public void ExitPort()
        {
            if (currentState == GameState.InPort)
            {
                SetSailingMode();
            }
        }

        public void StartDive()
        {
            if (currentState == GameState.Sailing)
            {
                SetDivingMode();
            }
        }

        public void EndDive()
        {
            if (currentState == GameState.Diving)
            {
                SetSailingMode();
            }
        }

        // Getter para estado actual
        public GameState GetCurrentState()
        {
            return currentState;
        }

        // Getter para isDiving
        public bool IsDiving()
        {
            return isDiving;
        }

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(10, 10, 300, 25), "STATE: " + currentState.ToString(), style);

            style.fontSize = 14;
            style.fontStyle = FontStyle.Normal;
            style.normal.textColor = Color.white;

            if (boatRb != null)
            {
                GUI.Label(new Rect(10, 35, 300, 20), "Boat Kinematic: " + boatRb.isKinematic.ToString(), style);
                GUI.Label(new Rect(10, 55, 300, 20), "Boat Velocity: " + boatRb.linearVelocity.magnitude.ToString("F2"), style);
            }

            if (boatMovement != null)
            {
                GUI.Label(new Rect(10, 75, 300, 20), "BoatMovement Active: " + boatMovement.IsActive().ToString(), style);
            }

            style.normal.textColor = Color.cyan;
            GUI.Label(new Rect(10, 100, 400, 20), "F5: Toggle Sailing/Diving", style);
        }
    }

    public enum GameState
    {
        Sailing,
        Diving,
        InPort,
        Paused
    }
}
































