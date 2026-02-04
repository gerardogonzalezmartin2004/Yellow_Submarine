using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    /// <summary>
    /// Controla el movimiento del buceador con física de agua realista.
    /// Incluye inercia, aceleración gradual y límite de 180°.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DiverMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Velocidad máxima de nado")]
        [SerializeField] private float swimSpeed = 5f;

        [Tooltip("Aceleración al empezar a nadar")]
        [SerializeField] private float acceleration = 8f;

        [Tooltip("Desaceleración al soltar los controles (inercia del agua)")]
        [SerializeField] private float waterDrag = 12f;

        [Header("Water Physics")]
        [Tooltip("Gravedad aplicada bajo el agua (más suave que en aire)")]
        [SerializeField] private float underwaterGravity = 2f;

        [Tooltip("Drag del Rigidbody en agua")]
        [SerializeField] private float rbDrag = 3f;

        [Header("Movement Constraints")]
        [Tooltip("Altura del agua (Y position)")]
        [SerializeField] private float waterSurfaceY = 0f;

        [Tooltip("Puede moverse hacia arriba hasta esta distancia del barco")]
        [SerializeField] private float minDepthFromBoat = 1f;

        [Header("References")]
        [SerializeField] private Transform boatTransform;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // Components
        private Rigidbody rb;
        private AbyssalReachControls controls;

        // State
        private Vector2 moveInput = Vector2.zero;
        private Vector2 currentVelocity = Vector2.zero;

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configurar Rigidbody
            rb.useGravity = false; // Usaremos gravedad custom
            rb.linearDamping = rbDrag;
            rb.constraints = RigidbodyConstraints.FreezePositionZ |
                           RigidbodyConstraints.FreezeRotationX |
                           RigidbodyConstraints.FreezeRotationY |
                           RigidbodyConstraints.FreezeRotationZ;

            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();
            controls.DiverControls.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            controls.DiverControls.Move.canceled += ctx => moveInput = Vector2.zero;
        }

        private void OnDisable()
        {
            controls.Disable();
        }

        private void FixedUpdate()
        {
            ApplyGravity();
            UpdateMovement();
            EnforceSurfaceLimit();
        }

        #endregion

        #region Movement Logic

        private void ApplyGravity()
        {
            // Aplicar gravedad suave bajo el agua
            rb.AddForce(Vector3.down * underwaterGravity, ForceMode.Force);
        }

        private void UpdateMovement()
        {
            // Calcular velocidad objetivo
            Vector2 targetVelocity = moveInput * swimSpeed;

            // Aplicar aceleración/desaceleración con inercia
            float rate = moveInput.magnitude > 0.01f ? acceleration : waterDrag;
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            // Aplicar límite de 180° (semicírculo)
            currentVelocity = ApplyHemisphereConstraint(currentVelocity);

            // Aplicar movimiento
            Vector3 movement = new Vector3(currentVelocity.x, currentVelocity.y, 0f) * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);

            if (showDebug && moveInput.magnitude > 0)
            {
                Debug.Log($"[Diver] Input: {moveInput} | Velocity: {currentVelocity} | Pos: {transform.position}");
            }
        }

        private Vector2 ApplyHemisphereConstraint(Vector2 velocity)
        {
            // Solo permitir movimiento hacia abajo o a los lados (180°)
            // NO permitir moverse hacia arriba más allá de cierto punto

            if (boatTransform == null) return velocity;

            // Si está intentando subir
            if (velocity.y > 0)
            {
                // Calcular distancia al barco
                float distanceToBoat = Mathf.Abs(transform.position.y - boatTransform.position.y);

                // Si está muy cerca del barco, bloquear movimiento hacia arriba
                if (distanceToBoat < minDepthFromBoat)
                {
                    velocity.y = Mathf.Min(velocity.y, 0f);
                }
            }

            return velocity;
        }

        private void EnforceSurfaceLimit()
        {
            // NO permitir salir del agua
            if (transform.position.y > waterSurfaceY)
            {
                Vector3 pos = transform.position;
                pos.y = waterSurfaceY;
                transform.position = pos;
                rb.position = pos;

                // Detener movimiento vertical
                Vector3 vel = rb.linearVelocity;
                vel.y = Mathf.Min(vel.y, 0f);
                rb.linearVelocity = vel;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Detiene el movimiento del buceador
        /// </summary>
        public void Stop()
        {
            currentVelocity = Vector2.zero;
            rb.linearVelocity = Vector3.zero;
        }

        /// <summary>
        /// Posiciona el buceador (ej: al empezar buceo)
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            position.z = 0f;
            position.y = Mathf.Min(position.y, waterSurfaceY); // Asegurar que está bajo agua
            transform.position = position;
            rb.position = position;
            Stop();
        }

        /// <summary>
        /// Asigna el transform del barco (para el constraint de 180°)
        /// </summary>
        public void SetBoatReference(Transform boat)
        {
            boatTransform = boat;
        }

        public Vector2 CurrentVelocity => currentVelocity;
        public Vector3 Position => transform.position;

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // Dibujar velocidad
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Vector3 vel3D = new Vector3(currentVelocity.x, currentVelocity.y, 0f);
                Gizmos.DrawRay(transform.position, vel3D);
            }

            // Dibujar límite de superficie
            Gizmos.color = Color.blue;
            float xPos = transform.position.x;
            Gizmos.DrawLine(
                new Vector3(xPos - 5f, waterSurfaceY, 0f),
                new Vector3(xPos + 5f, waterSurfaceY, 0f)
            );

            // Dibujar límite mínimo del barco
            if (boatTransform != null)
            {
                Gizmos.color = Color.yellow;
                float minY = boatTransform.position.y - minDepthFromBoat;
                Gizmos.DrawLine(
                    new Vector3(xPos - 3f, minY, 0f),
                    new Vector3(xPos + 3f, minY, 0f)
                );
            }
        }

        #endregion
    }
}