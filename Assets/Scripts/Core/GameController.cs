using UnityEngine;
using AbyssalReach.Gameplay;

namespace AbyssalReach.Core
{
    // En este script centralizamos la lógica de cambio entre los modos de juego (Navegación, Buceo, Tienda) y gestionamos el estado global del juego.
    public class GameController : MonoBehaviour
    {
        public enum GameState
        {
            Sailing,
            Diving,
            InPort,
            Paused
        }
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

        // FIX: Nuevo flag para saber si estamos en puerto
        private bool isInPort = false;

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
            Debug.Log("[" + gameObject.name + "] " + GetType().Name + " ENABLE - controls.DiverControls.enabled: " + controls.DiverControls.enabled);
            controls.Enable();

            // Suscribirse al botón de cambio de modo DESDE BOAT
            controls.BoatControls.StartDive.performed += OnStartDivePressed;

            // Por defecto, activar controles de gameplay
            SetInputToGameplay();
        }

        private void OnDisable()
        {
            controls.BoatControls.StartDive.performed -= OnStartDivePressed;
            controls.Disable();
        }

        private void Start()
        {
            SetSailingMode();
        }

        private void Update()
        {
            // Debug: F5 para cambiar modo
            // SOLO permite entrar a Diving desde Sailing
            // NO permite salir de Diving (eso se hace subiendo al barco)
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (showDebug)
                {
                    Debug.Log("[GameController] F5 presionado - Estado actual: " + currentState);
                }

                // Solo permitir cambiar a Diving si estamos en Sailing y NO en puerto
                if (currentState == GameState.Sailing && !isInPort)
                {
                    if (showDebug)
                    {
                        Debug.Log("[GameController] Cambiando a modo Diving");
                    }
                    SetDivingMode();
                }
                else if (currentState == GameState.Diving)
                {
                    if (showDebug)
                    {
                        Debug.Log("[GameController] No se puede volver a Sailing con F5 - Sube al barco primero");
                    }
                }
                else if (isInPort)
                {
                    if (showDebug)
                    {
                        Debug.Log("[GameController] No puedes bucear mientras estás en puerto");
                    }
                }
            }
        }

        #endregion

        #region Input Management

        private void OnStartDivePressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {

            // SOLO permitir cambio desde Sailing a Diving
            // NO permitir volver desde Diving (eso requiere subir al barco)
            if (currentState == GameState.Sailing && !isInPort)
            {
                if (showDebug)
                {
                    Debug.Log("[GameController] StartDive presionado - Cambiando a Diving");
                }
                SetDivingMode();
            }
            else if (currentState == GameState.Diving)
            {
                if (showDebug)
                {
                    Debug.Log("[GameController] Ya estás Diving - Sube al barco para volver");
                }
            }
            else if (isInPort)
            {
                if (showDebug)
                {
                    Debug.Log("[GameController] No puedes bucear en puerto");
                }
            }
        }

        // Cambiar inputs a UI (tiendas, menús)
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

            
            // Incluso si estábamos en modo diving, porque el toggle está en BoatControls
            controls.BoatControls.Enable();

            // Si estamos en modo diving, TAMBIÉN habilitar DiverControls
            if (isDiving)
            {
                controls.DiverControls.Enable();
            }
            else
            {
                controls.DiverControls.Disable();
            }

            if (showDebug)
            {
                Debug.Log("[GameController] Input cambiado a Gameplay (Boat: " + controls.BoatControls.enabled + ", Diver: " + controls.DiverControls.enabled + ")");
            }
        }

        #endregion

        #region Game State Management

        


        private void SetSailingMode()
        {
            Debug.LogError("=== SetSailingMode() LLAMADO ===");
            Debug.LogError(System.Environment.StackTrace);
            Debug.LogError("==============================");
            isDiving = false;
            currentState = GameState.Sailing;

            // Activar cámara de barco
            if (boatCamera != null)
            {
                boatCamera.SetActive(true);
            }
            if (diverCamera != null)
            {
                diverCamera.SetActive(false);
            }

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

            // Activar movimiento del barco
            if (boatMovement != null)
            {
                boatMovement.SetMovementActive(true);
            }

            // Rigidbody del barco normal
            if (boatRb != null)
            {
                boatRb.isKinematic = false;
            }

            // Cambiar inputs
            controls.BoatControls.Enable();
            controls.DiverControls.Disable();
            controls.UI.Disable();

            if (showDebug)
            {
                Debug.Log("[GameController] Modo Navegación activado");
            }
        }

        private void SetDivingMode()
        {
            isDiving = true;
            currentState = GameState.Diving;

            // Activar cámara de buzo
            if (boatCamera != null)
            {
                boatCamera.SetActive(false);
            }
            if (diverCamera != null)
            {
                diverCamera.SetActive(true);
            }

            // Congelar barco
            if (boatRb != null)
            {
                boatRb.linearVelocity = Vector3.zero;
                boatRb.angularVelocity = Vector3.zero;
                boatRb.isKinematic = true;
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

            // FIX: Mantener BoatControls activo para el botón de toggle
            // Pero TAMBIÉN activar DiverControls
            controls.BoatControls.Enable();
            controls.DiverControls.Enable();
            controls.UI.Disable();

            if (showDebug)
            {
                Debug.Log("[GameController] Modo Buceo activado");
            }
        }

        #endregion

        #region Port Management

        public void EnterPort()
        {
            if (currentState == GameState.Sailing)
            {
                currentState = GameState.InPort;
                isInPort = true; // FIX: Marcar que estamos en puerto

                if (showDebug)
                {
                    Debug.Log("[GameController] Entrando al puerto");
                }
            }
        }

        public void ExitPort()
        {
            if (currentState == GameState.InPort)
            {
                currentState = GameState.Sailing;
                isInPort = false; // FIX: Ya no estamos en puerto

                if (showDebug)
                {
                    Debug.Log("[GameController] Saliendo del puerto");
                }

                // Asegurar que el barco puede moverse
                if (boatMovement != null)
                {
                    boatMovement.SetMovementActive(true);
                }

                if (boatRb != null)
                {
                    boatRb.isKinematic = false;
                }
            }
        }

        #endregion

        #region Dive Management

        public void StartDive()
        {
            if (currentState == GameState.Sailing && !isInPort)
            {
                SetDivingMode();
            }
        }

        public void EndDive()
        {
            Debug.LogError("=== EndDive() LLAMADO ===");
            Debug.LogError(System.Environment.StackTrace);
            Debug.LogError("========================");
            if (currentState == GameState.Diving)
            {
                SetSailingMode();
            }
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

        public bool IsInPort()
        {
            return isInPort;
        }

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
                GUI.Label(new Rect(10, 35, 300, 20), "Boat Kinematic: " + boatRb.isKinematic.ToString(), style);
                GUI.Label(new Rect(10, 55, 300, 20), "Boat Velocity: " + boatRb.linearVelocity.magnitude.ToString("F2"), style);
            }

            if (boatMovement != null)
            {
                GUI.Label(new Rect(10, 75, 300, 20), "BoatMovement Active: " + boatMovement.IsActive().ToString(), style);
            }

            // FIX: Mostrar qué Input Maps están activos
            style.normal.textColor = Color.cyan;

            GUI.Label(new Rect(10, 95, 300, 20), "In Port: " + isInPort.ToString(), style);
            GUI.Label(new Rect(10, 115, 300, 20), "Boat Controls: " + controls.BoatControls.enabled.ToString(), style);
            GUI.Label(new Rect(10, 135, 300, 20), "Diver Controls: " + controls.DiverControls.enabled.ToString(), style);
            GUI.Label(new Rect(10, 155, 300, 20), "UI Controls: " + controls.UI.enabled.ToString(), style);

            GUI.Label(new Rect(10, 180, 400, 20), "F5: Toggle Sailing/Diving", style);
        }

        #endregion
    }
}

