using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
       
    [RequireComponent(typeof(Rigidbody))] // Por si acaso
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

        [Header("Water Physics")]
        [Tooltip("Drag cuando está en agua")]
        [SerializeField] private float waterDrag = 1.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

       
        private Rigidbody rb;
        private AbyssalReachControls controls;

        
        private float currentSpeed = 0f;
        private float moveInput = 0f;

        #region Ciclo de Vida

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configurazao Rigidbody 
            rb.useGravity = false;
            rb.linearDamping = waterDrag;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

            // Inicializar controles
            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.Enable(); // Aseguramos que el mapa de controles del barco está activo

            controls.BoatControls.Movement.performed += ctx => moveInput = ctx.ReadValue<float>();
            controls.BoatControls.Movement.canceled += ctx => moveInput = 0f;

            if (showDebug)
            {
                Debug.Log("[Boat] Controles habilitados");
            }
        }

        private void OnDisable()
        {
            // Limpiamos el input para evitar que el barco siga moviéndse después de desactivar los controles
            moveInput = 0f;
            currentSpeed = 0f;

            controls.BoatControls.Disable();
            controls.Disable();
        }

        private void FixedUpdate()
        {
            if (!enabled)
            {
                return;// Evitar actualizar si el script está deshabilitado
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

            // Si nos intentamos mover (velocidad objetivo mayor que casi 0)
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

            // Aplicar movimiento
            Vector3 movement = Vector3.right * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);

            if (showDebug)
            {
                Debug.Log("[Boat] Input: {"+ moveInput+ ":F2} | Speed: {"+ currentSpeed+":F2} | Pos: {" + transform.position.x +":F2}");                
            }
        }

        #endregion

        #region Aplicaciones

      
        
        // Detiene el movimiento del barco
        public void Stop()
        {
            currentSpeed = 0f;
            moveInput = 0f; // Reseteamos el input

            if (rb != null)
            {
               
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
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

        public float CurrentSpeed ()
        {
             return currentSpeed; 
        }

        public Vector3 Position ()
        {
            return transform.position; 
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug || !Application.isPlaying) 
            {
                return;
            }

            if (currentSpeed > 0)
            {
                Gizmos.color = Color.green; // Avanza - verde
            }
            else if (currentSpeed < 0)
            {
                Gizmos.color = Color.red;   // Retrocede - rojo
            }
            else
            {
                Gizmos.color = Color.yellow; // Quieto - amarillo
            }

            Gizmos.DrawRay(transform.position, Vector3.right * currentSpeed);

            // Dibujamos una bolita verde si el script está activo (navegando) o gris si no (buceando)
            Gizmos.color = enabled ? Color.green : Color.gray;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }

        #endregion
    }
}