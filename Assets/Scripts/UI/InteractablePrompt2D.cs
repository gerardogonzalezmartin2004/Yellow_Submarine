using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{

    [RequireComponent(typeof(SpriteRenderer))]
    public class InteractablePrompt2D : MonoBehaviour
    {
        // Define quién tiene permiso para activar este aviso (Barco, Buzo o ambos)
        public enum InteractorType { BoatOnly, DiverOnly, Both }

        // Define los tres estados visuales del icono
        private enum State
        {
            Hidden, 
            Far,    
            Near    
        }

        [Header(" Interactor Settings")]
        [Tooltip("¿Qué personaje puede activar este prompt?")]
        [SerializeField] private InteractorType allowedInteractor = InteractorType.BoatOnly;

        [Header(" Sprites")]
        [SerializeField] private Sprite farSprite;
        [SerializeField] private Sprite nearKeyboardSprite;
        [SerializeField] private Sprite nearGamepadSprite;

        [Header(" Size & Distance Settings")]
        // Radios de detección. El exterior siempre debe ser mayor que el interior.
        [SerializeField] private float outerRadius = 5f;
        [SerializeField] private float innerRadius = 2f;

        // Escala que tendrá el SpriteRenderer cuando esté visible.
        // Sirve para ajustar el tamaño del icono sin tocar el Transform en Unity.
        [SerializeField] private Vector3 baseIconScale = new Vector3(1f, 1f, 1f);

        [Header(" Interaction Event")]
        [Tooltip("Función que se ejecutará al pulsar el botón cuando estás cerca.")]
        // UnityEvent permite arrastrar funciones desde el Inspector 
        // sin necesidad de "hardcodear" la lógica aquí.
        public UnityEvent OnInteract;

        // --- Variables Internas ---
        private SpriteRenderer spriteRenderer;
        private State currentState = State.Hidden; 
        private Transform boatTransform;           // Referencia cacheada del barco
        private Transform diverTransform;          // Referencia cacheada del buzo
        private AbyssalReachControls controls;     // Referencia al Input System autogenerado
        private InputAction interactAction;        // La acción específica que estamos escuchand
        private bool isInRange = false;            
        private bool usingGamepad = false;        

        #region Unity Lifecycle

        private void Awake()
        {
            // Cacheamos el componente al despertar para no usar GetComponent en el Update
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Forzamos el color a blanco puro (para que el Sprite original se vea tal cual)
            // Y lo apagamos para que no sea visible al iniciar la escena.
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = false;
        }

        private void Start()
        {
            // Intentamos conectarnos al GameController centralizado para obtener los inputs
            if (GameController.Instance != null)
            {
                controls = GameController.Instance.GetControls();
                if (controls != null) SubscribeInput();
            }
        }

        // OnEnable y OnDisable son vitales con el New Input System.
        // Si el objeto se apaga, dejamos de escuchar el botón para evitar memory leaks o bugs.
        private void OnEnable()
        {
            if (controls != null) SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            // Buscamos a los jugadores. Lo hacemos en el Update por si el buzo 
            // estaba desactivado en el Start
            FindPlayers();

            // Averiguamos quién está jugando 
            Transform player = GetActivePlayer();

            // Si no hay jugador válido, ocultamos el icono y salimos del Update temprano
            if (player == null)
            {
                if (currentState != State.Hidden) ChangeState(State.Hidden);
                return;
            }

            // Calculamos la distancia real en línea recta entre el icono y el jugador
            float distance = Vector2.Distance(transform.position, player.position);

            //  Determinamos en qué estado deberíamos estar según la distancia
            State newState = GetStateFromDistance(distance);

            //  Si el estado que deberíamos tener es distinto al actual, hacemos la transición
            if (newState != currentState)
            {
                ChangeState(newState);
            }

            //  Lógica continua mientras estemos en la zona 
            if (currentState == State.Near)
            {
                UpdateDeviceSprite(); // Vigila si el jugador coge el mando o el teclado
                isInRange = true;     // Permite que el botón funcione
            }
            else
            {
                isInRange = false;    // Bloquea el botón si estás lejos
            }
        }

        #endregion

        #region Logic & State Management

        // Busca los Transforms usando Tags. Es más barato guardarlos en variables 
        // una vez encontrados que buscarlos cada frame.
        private void FindPlayers()
        {
            if (boatTransform == null)
            {
                GameObject boat = GameObject.FindGameObjectWithTag("Boat");
                if (boat != null) boatTransform = boat.transform;
            }
            if (diverTransform == null)
            {
                GameObject diver = GameObject.FindGameObjectWithTag("Diver");
                if (diver != null) diverTransform = diver.transform;
            }
        }

        // Filtro de seguridad: Devuelve el Transform del jugador SOLO SI 
        //  Ese jugador existe
        //  El estado actual del juego coincide con el tipo de interactor permitido
        private Transform GetActivePlayer()
        {
            if (GameController.Instance == null) return null;
            GameController.GameState gameState = GameController.Instance.GetCurrentState();

            if (allowedInteractor == InteractorType.BoatOnly && gameState == GameController.GameState.Sailing) return boatTransform;
            if (allowedInteractor == InteractorType.DiverOnly && gameState == GameController.GameState.Diving) return diverTransform;
            if (allowedInteractor == InteractorType.Both) return (gameState == GameController.GameState.Sailing) ? boatTransform : diverTransform;

            return null;
        }

        // Función matemática simple que traduce distancia en Estados
        private State GetStateFromDistance(float distance)
        {
            if (distance <= innerRadius) return State.Near;
            if (distance <= outerRadius) return State.Far;
            return State.Hidden;
        }

        // Aplica los cambios visuales cuando cambiamos de zona 
        private void ChangeState(State newState)
        {
            currentState = newState;

            switch (currentState)
            {
                case State.Hidden:
                    spriteRenderer.enabled = false; // Apagamos el renderer por completo para ahorrar rendimiento
                    break;

                case State.Far:
                    if (farSprite != null) spriteRenderer.sprite = farSprite;
                    transform.localScale = baseIconScale; // Aplicamos el tamaño dictado en el Inspector
                    spriteRenderer.enabled = true; // Encendemos el renderer
                    break;

                case State.Near:
                    UpdateDeviceSprite(true); // true = Forzamos a que ponga el icono de Teclado o Mando instantáneamente
                    transform.localScale = baseIconScale;
                    spriteRenderer.enabled = true;
                    break;
            }
        }

        // Se encarga de cambiar entre el icono de la Tecla 'E' y el Botón 'X'
        // El parámetro 'forceUpdate' se usa cuando acabamos de entrar en la zona para que no espere a que movamos el mando.
        private void UpdateDeviceSprite(bool forceUpdate = false)
        {
            bool nowUsingGamepad = IsGamepadActive();

            // Solo cambiamos el sprite si has cambiado de dispositivo (para no sobreescribir la imagen cada frame inútilmente)
            if (forceUpdate || nowUsingGamepad != usingGamepad)
            {
                usingGamepad = nowUsingGamepad;
                spriteRenderer.sprite = usingGamepad ? nearGamepadSprite : nearKeyboardSprite;
            }
        }

        // Revisa directamente el Hardware de Unity para ver si se está usando un mando AHORA MISMO
        private bool IsGamepadActive()
        {
            if (Gamepad.current == null) return false;

            // Si el joystick se ha movido este frame...
            if (Gamepad.current.wasUpdatedThisFrame) return true;

            // O si algún botón del mando está siendo pulsado...
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is UnityEngine.InputSystem.Controls.ButtonControl btn && btn.isPressed) return true;
            }
            return false;
        }

        #endregion

        #region Input Handling

        
        private void SubscribeInput()
        {
            if (controls == null) return;

            // Decidimos a qué botón hacemos caso dependiendo de a quién configuramos en el inspector
            if (allowedInteractor == InteractorType.BoatOnly)
            {
                interactAction = controls.BoatControls.Interact;
            }
            else if (allowedInteractor == InteractorType.DiverOnly)
            {
                //  Ahora usa Interact en vez de Ascend
                interactAction = controls.DiverControls.Interact;
            }

            // += significa "cuando se ejecute 'performed', ejecuta también 'OnInteract_Performed'"
            if (interactAction != null) interactAction.performed += OnInteract_Performed;
        }

        // Desvincula la función para evitar fugas de memoria
        private void UnsubscribeInput()
        {
            if (interactAction != null)
            {
                interactAction.performed -= OnInteract_Performed;
                interactAction = null;
            }
        }

        // Esta función se dispara SOLA cuando el jugador pulsa el botón configurado
        private void OnInteract_Performed(InputAction.CallbackContext context)
        {
            // Bloqueo de seguridad: Si has pulsado el botón, pero estás fuera del círculo interior, no hagas nada.
            if (!isInRange || GameController.Instance == null) return;

           

            // ?.Invoke() ejecuta todos los métodos que hayas arrastrado al evento OnInteract en el Inspector de Unity.
            OnInteract?.Invoke();
        }

        #endregion

        #region Editor Gizmos

        // Esta función solo se ejecuta en el Editor de Unity cuando seleccionas el objeto.
        // Dibuja los círculos de colores para que puedas ajustar las distancias visualmente sin tener que probar el juego.
        private void OnDrawGizmosSelected()
        {
            // Círculo exterior 
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, outerRadius);

            // Círculo interior 
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }

        #endregion
    }
}