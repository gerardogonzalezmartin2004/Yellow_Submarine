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

        [Header("Boat Visuals")]
        [SerializeField] private GameObject barco;

        [Tooltip("Inclinación lateral")]
        [SerializeField] private float maxTiltZ = 12f;

        [Tooltip("Elevación de la punta")]
        [SerializeField] private float maxTiltX = 6f;

        [Tooltip("Velocidad de giro visual")]
        [SerializeField] private float turnSpeed = 10f;

        [Tooltip("Velocidad del balanceo")]
        [SerializeField] private float bobbingSpeed = 2f;

        [Tooltip("Intensidad del balanceo")]
        [SerializeField] private float bobbingAmount = 1.5f;

        private Rigidbody rb;
        private AbyssalReachControls controls;

        private float currentSpeed = 0f;
        private float moveInput = 0f;
        private bool isActive = true;

        private Quaternion targetRotation;
        private float targetYRotation = 0f;

        #region Unity ciclo de vida

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configuración del Rigidbody
            rb.useGravity = false;
            rb.linearDamping = waterDrag;

            rb.constraints =
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY;

            // Inicializar controles
            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.BoatControls.Enable();

            controls.BoatControls.Movement.performed += OnMovementPerformed;
            controls.BoatControls.Movement.canceled += OnMovementCanceled;
        }

        private void OnDisable()
        {
            moveInput = 0f;
            currentSpeed = 0f;

            controls.BoatControls.Movement.performed -= OnMovementPerformed;
            controls.BoatControls.Movement.canceled -= OnMovementCanceled;

            controls.BoatControls.Disable();
            controls.Disable();
        }

        private void FixedUpdate()
        {
            if (!enabled || !isActive)
            {
                return;
            }

            UpdateMovement();
            UpdateBoatVisuals();
        }

        #endregion

        #region Input

        private void OnMovementPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!isActive)
            {
                return;
            }

            Vector2 inputVector = context.ReadValue<Vector2>();
            float rawInput = inputVector.x;

            if (Mathf.Abs(rawInput) < inputDeadzone)
            {
                moveInput = 0f;
            }
            else
            {
                float sign = Mathf.Sign(rawInput);
                float magnitude = Mathf.Abs(rawInput);

                float normalized =
                    (magnitude - inputDeadzone) /
                    (1f - inputDeadzone);

                moveInput = sign * Mathf.Clamp01(normalized);
            }
        }

        private void OnMovementCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            moveInput = 0f;
        }

        #endregion

        #region Movement Logic

        private void UpdateMovement()
        {
            float targetSpeed = moveInput * maxSpeed;

            float accelRate;

            if (Mathf.Abs(targetSpeed) > 0.01f)
            {
                accelRate = acceleration;
            }
            else
            {
                accelRate = deceleration;
            }

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                accelRate * Time.fixedDeltaTime
            );

            Vector3 movement =
                Vector3.right *
                currentSpeed *
                Time.fixedDeltaTime;

            rb.MovePosition(rb.position + movement);
        }

        #endregion

        #region Visuals

        private void UpdateBoatVisuals()
        {
            if (barco == null)
            {
                return;
            }

            float normalizedSpeed = currentSpeed / maxSpeed;

            // Dirección visual
            if (currentSpeed > 0.05f)
            {
                targetYRotation = 0f;
            }
            else if (currentSpeed < -0.05f)
            {
                targetYRotation = 180f;
            }

            // Inclinación lateral
            float tiltZ;

            // Si mira a la izquierda, invertimos el tilt
            if (targetYRotation == 180f)
            {
                tiltZ = -normalizedSpeed * maxTiltZ;
            }
            else
            {
                tiltZ = normalizedSpeed * maxTiltZ;
            }

            // Punta arriba
            float tiltX = -Mathf.Abs(normalizedSpeed) * maxTiltX;

            // Balanceo
            float bobbing =
                Mathf.Sin(Time.time * bobbingSpeed) *
                bobbingAmount;

            tiltX += bobbing;

            // Rotación objetivo
            targetRotation = Quaternion.Euler(
                tiltX,
                targetYRotation,
                tiltZ
            );

            // Suavizado
            barco.transform.localRotation = Quaternion.Slerp(
                barco.transform.localRotation,
                targetRotation,
                turnSpeed * Time.fixedDeltaTime
            );
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

        // Activa o desactiva el control
        public void SetMovementActive(bool active)
        {
            isActive = active;

            if (!active)
            {
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
            if (!Application.isPlaying)
            {
                return;
            }

            if (currentSpeed > 0)
            {
                Gizmos.color = Color.green;
            }
            else if (currentSpeed < 0)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }

            Gizmos.DrawRay(
                transform.position,
                Vector3.right * currentSpeed
            );

            Gizmos.color = isActive
                ? Color.green
                : Color.gray;

            Gizmos.DrawWireSphere(
                transform.position + Vector3.up * 2f,
                0.3f
            );
        }

        #endregion
    }
}