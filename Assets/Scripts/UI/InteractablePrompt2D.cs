using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using AbyssalReach.Core;

namespace AbyssalReach.UI
{

    [RequireComponent(typeof(SpriteRenderer))]
    public class InteractablePrompt2D : MonoBehaviour
    {
        
        public enum InteractorType { BoatOnly, DiverOnly, Both }

      
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
        
        [SerializeField] private float outerRadius = 5f;
        [SerializeField] private float innerRadius = 2f;

       
        [SerializeField] private Vector3 baseIconScale = new Vector3(1f, 1f, 1f);

        [Header(" Interaction Event")]
        [Tooltip("Función que se ejecutará al pulsar el botón cuando estás cerca.")]
      
        public UnityEvent OnInteract;

      
        private SpriteRenderer spriteRenderer;
        private State currentState = State.Hidden; 
        private Transform boatTransform;           
        private Transform diverTransform;          
        private AbyssalReachControls controls;     
        private InputAction interactAction;        
        private bool isInRange = false;            
        private bool usingGamepad = false;         

        #region Unity Lifecycle

        private void Awake()
        {
           
            spriteRenderer = GetComponent<SpriteRenderer>();

           
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = false;
        }

        private void Start()
        {
            
            if (GameController.Instance != null)
            {
                controls = GameController.Instance.GetControls();
                if (controls != null) SubscribeInput();
            }
        }

      
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
           
            FindPlayers();

            
            Transform player = GetActivePlayer();

          
            if (player == null)
            {
                if (currentState != State.Hidden) ChangeState(State.Hidden);
                return;
            }

           
            float distance = Vector2.Distance(transform.position, player.position);

          
            State newState = GetStateFromDistance(distance);

           
            if (newState != currentState)
            {
                ChangeState(newState);
            }

          
            if (currentState == State.Near)
            {
                UpdateDeviceSprite(); 
                isInRange = true;    
            }
            else
            {
                isInRange = false;   
            }
        }

        #endregion

        #region Logic & State Management

       
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

        
        private Transform GetActivePlayer()
        {
            if (GameController.Instance == null) return null;
            GameController.GameState gameState = GameController.Instance.GetCurrentState();

            if (allowedInteractor == InteractorType.BoatOnly && gameState == GameController.GameState.Sailing) return boatTransform;
            if (allowedInteractor == InteractorType.DiverOnly && gameState == GameController.GameState.Diving) return diverTransform;
            if (allowedInteractor == InteractorType.Both) return (gameState == GameController.GameState.Sailing) ? boatTransform : diverTransform;

            return null;
        }

        private State GetStateFromDistance(float distance)
        {
            if (distance <= innerRadius) return State.Near;
            if (distance <= outerRadius) return State.Far;
            return State.Hidden;
        }

      
        private void ChangeState(State newState)
        {
            currentState = newState;

            switch (currentState)
            {
                case State.Hidden:
                    spriteRenderer.enabled = false;
                    break;

                case State.Far:
                    if (farSprite != null) spriteRenderer.sprite = farSprite;
                    transform.localScale = baseIconScale; 
                    spriteRenderer.enabled = true; 
                    break;

                case State.Near:
                    UpdateDeviceSprite(true); 
                    transform.localScale = baseIconScale;
                    spriteRenderer.enabled = true;
                    break;
            }
        }

        
        private void UpdateDeviceSprite(bool forceUpdate = false)
        {
            bool nowUsingGamepad = IsGamepadActive();

           
            if (forceUpdate || nowUsingGamepad != usingGamepad)
            {
                usingGamepad = nowUsingGamepad;
                spriteRenderer.sprite = usingGamepad ? nearGamepadSprite : nearKeyboardSprite;
            }
        }

        // Revisa directamente el Hardware de Unity para ver si se está usando un mando
        private bool IsGamepadActive()
        {
            if (Gamepad.current == null) return false;

            
            if (Gamepad.current.wasUpdatedThisFrame) return true;

          
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

           
            if (allowedInteractor == InteractorType.BoatOnly)
            {
                interactAction = controls.BoatControls.Interact;
            }
            else if (allowedInteractor == InteractorType.DiverOnly)
            {
               
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

      
        private void OnInteract_Performed(InputAction.CallbackContext context)
        {
         
            if (!isInRange || GameController.Instance == null) return;

            Debug.Log("[InteractablePrompt2D] ¡BOTÓN PULSADO! Ejecutando OnInteract"); 

            // ?.Invoke() ejecuta todos los métodos que hayas arrastrado al evento OnInteract en el Inspector de Unity.
            OnInteract?.Invoke();
        }

        #endregion

        #region Editor Gizmos

     
        private void OnDrawGizmosSelected()
        {
          
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, outerRadius);

           
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }

        #endregion
    }
}