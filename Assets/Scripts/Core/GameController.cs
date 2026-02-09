using UnityEngine;
using AbyssalReach.Gameplay;

namespace AbyssalReach.Core
{
    // En este script centralizamos la lógica de cambio entre los modos de juego (Navegación, Buceo, Tienda) y gestionamos el estado global del juego.
    public class GameController : MonoBehaviour
    {
        // Singleton 
        private static GameController instance;

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
        [SerializeField] private GameObject boatCamera;
        [SerializeField] private GameObject diverCamera;

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

        #region Unity ciclo de vida

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

            // Inicializar controles
            controls = new AbyssalReachControls();

            // Cachear referencias
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

            // Suscripción al botón de Empezar Inmersión
            controls.BoatControls.StartDive.performed += OnStartDivePressed;

            // Por defecto activamos los controles de juego
            SetInputToGameplay();
        }

        private void OnDisable()
        {
            controls.BoatControls.StartDive.performed -= OnStartDivePressed;
            controls.Disable();
        }

        private void Start()
        {
            // Empezar en modo navegación
            SetSailingMode();
        }

        private void Update()
        {
            // Debug rápido por si queremos probar el cambio de modo sin usar el mando. IMPORTANTE, YA QUE HAY QUE QUITARLOS TRAS LAS PREUBAS O SINOS EL PROFE EDU..
            if (Input.GetKeyDown(KeyCode.F5))
            {
                ToggleDiving();
            }
        }

        #endregion

        #region Input Logic

        private void OnStartDivePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Solo permitimos cambiar modo si estamos en un estado de Gameplay válido. Evita que cambie si estás en la tienda o pausado
            if (currentState == GameState.Sailing || currentState == GameState.Diving)
            {
                ToggleDiving();
            }
        }

        

        // Estos métodos permiten que la Tienda (PortArea) pida el control del mando
        public void SetInputToUI()
        {
            controls.BoatControls.Disable();
            controls.DiverControls.Disable();
            controls.UI.Enable();

            if (showDebug)
            {
                Debug.Log("[GameController] Input cambiado a UI");
            }
        }

        public void SetInputToGameplay()
        {
            controls.UI.Disable();

            // Reactivamos el mapa correcto según el estado actual
            if (isDiving)
            {
                controls.DiverControls.Enable();
            }
            else
            {
                controls.BoatControls.Enable();
            }

            if (showDebug)
            {
                Debug.Log("[GameController] Input cambiado a Gameplay");
            }
        }

        #endregion

        #region Game State Logic

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

        public void SetSailingMode()
        {
            isDiving = false;
            currentState = GameState.Sailing;
            boatCamera.SetActive(true);
            diverCamera.SetActive(false);

            //  Activar barco y sus físicas
            if (boat != null)
            {
                boat.SetActive(true);
            }

            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            // Desactivar buzo y cable
            if (diver != null)
            {
                diver.SetActive(false);
            }

            if (tetherSystem != null)
            {
                tetherSystem.SetActive(false);
            }

            // Cambiar inputs
            controls.BoatControls.Enable();
            controls.DiverControls.Disable();
            controls.UI.Disable();

            if (showDebug)
            {
                Debug.Log("[GameController] Modo Navegación Activado");
            }
        }

        public void SetDivingMode()
        {
            isDiving = true;
            currentState = GameState.Diving;
            boatCamera.SetActive(false);
            diverCamera.SetActive(true);

            // Congelar Barco
            if (boatMovement != null)
            {
                boatMovement.Stop();
                boatMovement.SetMovementActive(false);
            }

            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
                boatRb.angularVelocity = Vector3.zero;
                boatRb.isKinematic = true;
            }

            // Activar y Posicionar Buzo
            if (diver != null && boat != null)
            {
                Vector3 spawnPos = boat.transform.position;
                if (boatAnchor != null)
                {
                    spawnPos = boatAnchor.position;
                }

                // Spawneamos un poco más abajo para no chocar
                spawnPos.y = spawnPos.y - 2f;

                diver.SetActive(true);

                if (diverMovement != null)
                {
                    diverMovement.SetPosition(spawnPos);
                    diverMovement.SetBoatReference(boat.transform);
                }
            }

            // Activar Cable
            if (tetherSystem != null)
            {
                tetherSystem.SetActive(true);
            }

            // Cambiar Inputs
            controls.BoatControls.Disable();
            controls.DiverControls.Enable();
            controls.UI.Disable();

            if (showDebug)
            {
                Debug.Log("[GameController] Modo Buceo Activado");
            }
        }

        public void EnterPort()
        {
            if (currentState == GameState.Sailing)
            {
                currentState = GameState.InPort;

                // Detener barco
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

                //No hay que olvidar q el input a UI lo gestiona PortArea llamando a SetInputToUI()
            }
        }

        public void ExitPort()
        {
            if (currentState == GameState.InPort)
            {
                // Volver a modo navegación normal
                SetSailingMode();
            }
        }

        // Estos métodos pueden ser llamados por el botón de buceo del mando o por la UI para cambiar entre modos. 
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
        public enum GameState
        {
            Sailing,
            Diving,
            InPort,
            Paused
        }

        #endregion

        #region Getters

        public GameState GetCurrentState()
        {
            return currentState;
        }

        public bool IsDiving()
        {
            return isDiving;
        }

        // Permite a otros scripts como el del barco acceder a la instancia centralizada de controles
        public AbyssalReachControls GetControls()
        {
            return controls;
        }

        #endregion

        #region Debug (GUI)

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
                GUI.Label(new Rect(10, 55, 300, 20), "Boat Velocity: " + boatRb.linearVelocity.magnitude.ToString("F2"), style);
            }

            style.normal.textColor = Color.cyan;
            GUI.Label(new Rect(10, 100, 400, 20), "F5: Cambiar Sailing/Diving", style);
        }

        #endregion
    }

    
}