using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoatMovement : MonoBehaviour
    {
        // Controla el movimiento horizontal del barco
        [Header("Movement Settings")]
        [Tooltip("Velocidad máxima del barco en m/s")]
        [SerializeField] private float maxSpeed = 8f;

        [Tooltip("Qué tan rápido acelera")]
        [SerializeField] private float acceleration = 15f;

        [Tooltip("Qué tan rápido frena")]
        [SerializeField] private float deceleration = 20f;

        [Header("Input Settings")]
        [Tooltip("Zona muerta del input (ignora valores menores a este)")]
        [SerializeField] private float inputDeadzone = 0.15f;

        [Header("Water Physics")]
        [Tooltip("Drag cuando está en agua")]
        [SerializeField] private float waterDrag = 1.5f;

        private Rigidbody rb;
        private AbyssalReachControls controls;

        private float currentSpeed = 0f;
        private float moveInput = 0f;
        private bool isActive = true;

        #region Unity ciclo de vida

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configuración del Rigidbody
            rb.useGravity = false;
            rb.linearDamping = waterDrag;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            // Inicializar controles
            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.Enable();

            // Suscripción a eventos del Input System 
            controls.BoatControls.Movement.performed += OnMovementPerformed; // .performed se llama cuando hay un cambio en el input (ej: el jugador está moviendo el joystick ahora mismo)
            controls.BoatControls.Movement.canceled += OnMovementCanceled; //. canceled se llama cuando el jugador suelta el joystick o la tecla, es decir, deja de dar input
            // Y el += es para suscribir la función a ese evento, así cada vez que el jugador mueva el joystick se ejecutará OnMovementPerformed, y cada vez que lo suelte se ejecutará OnMovementCanceled
        }
        private void OnDisable()
        {
            // Limpiamos el input al desactivar
            moveInput = 0f;
            currentSpeed = 0f;

            // Desuscripción de eventos y asi e evitamos errores de memoria
            controls.BoatControls.Movement.performed -= OnMovementPerformed;
            controls.BoatControls.Movement.canceled -= OnMovementCanceled;

            controls.BoatControls.Disable();
            controls.Disable();
        }
        private void OnMovementPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!isActive)
            {
                return;
            }

            Vector2 inputVector = context.ReadValue<Vector2>();
            float rawInput = inputVector.x; // Solo usar eje X

            // Aplicar deadzone: Si el valor absoluto es menor que el umbral, ignorar
            if (Mathf.Abs(rawInput) < inputDeadzone)
            {
                moveInput = 0f;
            }
            else
            {
                // Normalizar el input fuera de la deadzone
                // Mapear de [deadzone, 1.0] a [0, 1.0] para suavizar la respuesta
                float sign = Mathf.Sign(rawInput);
                float magnitude = Mathf.Abs(rawInput);
                float normalized = (magnitude - inputDeadzone) / (1f - inputDeadzone);
                moveInput = sign * Mathf.Clamp01(normalized);

            }
        }

        private void OnMovementCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            moveInput = 0f;

        }
      
        private void FixedUpdate()
        {
            // Evitar actualizar si el script está deshabilitado o inactivo
            if (!enabled || !isActive)
            {
                return;
            }
            UpdateMovement();
        }

        #endregion             
       
        #region Movement Logic

        private void UpdateMovement()
        {
            // Acelerar o frenar según el input
            float targetSpeed = moveInput * maxSpeed;
            float accelRate;

            // Si nos intentamos mover (> 0.01), aceleramos. Si no, frenamos.
            if (Mathf.Abs(targetSpeed) > 0.01f)
            {
                accelRate = acceleration;
            }
            else
            {
                accelRate = deceleration;
            }

            // MoveTowards suaviza el cambio de velocidad
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);

            // Aplicar movimiento físico
            Vector3 movement = Vector3.right * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        #endregion

        #region Aplicaciones 

        // Detiene el movimiento del barco completamente
        public void Stop()
        {
            currentSpeed = 0f;
            moveInput = 0f;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // Activa o desactiva el control el GameController
        public void SetMovementActive(bool active)
        {
            isActive = active;

            if (!active)
            {
                // Si se desactiva, reseteamos el input interno
                moveInput = 0f;
            }
        }

        // Teletransporta el barco a una posición
        public void SetPosition(Vector3 position)
        {
            position.z = 0f;
            transform.position = position;
            rb.position = position;
            Stop();
        }

        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public bool IsActive()
        {
            return isActive;
        }

        #endregion

        #region Debug (Gizmos) 

        private void OnDrawGizmos()
        {
            // Solo dibujamos si estamos jugando para ver la velocidad real, sino veriamos una pelota amarilla 
            if (!Application.isPlaying)
            {
                return;
            }

            // Colores según dirección
            if (currentSpeed > 0)
            {
                Gizmos.color = Color.green; // Avanza
            }
            else if (currentSpeed < 0)
            {
                Gizmos.color = Color.red;   // Retrocede
            }
            else
            {
                Gizmos.color = Color.yellow; // Quieto
            }

            // La longitud representa la velocidad actual, y el color representa la dirección. (Origen,Vector3.right * currentSpeed (Dirección y Longitud))
            Gizmos.DrawRay(transform.position, Vector3.right * currentSpeed);

            // Bolita de estado: Verde si tengo el control, gris si no
            if (isActive)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.gray;
            }
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);//(Centro, Radio). Y la funcion es para dibujar una esfera hueca encima del barco que nos indica si el control está activo o no.
        }

        #endregion
    }
}