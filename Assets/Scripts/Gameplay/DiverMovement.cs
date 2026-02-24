using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class DiverMovement : MonoBehaviour
    {
        // Controla el movimiento del buceador con física de agua.

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

        private Rigidbody rb;
        private AbyssalReachControls controls;

        private Vector2 moveInput = Vector2.zero;
        private Vector2 currentVelocity = Vector2.zero;

        #region Unity ciclo de vida

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configurar Rigidbody
            rb.useGravity = false;
            rb.linearDamping = rbDrag;

            // Congelar rotaciones para que no de vueltas como un loco
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX |RigidbodyConstraints.FreezeRotationY |RigidbodyConstraints.FreezeRotationZ;

            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();

            // Suscripción a los eventos de Input: Esta explicado en BoatMovement.
            controls.DiverControls.Move.performed += OnMovePerformed;
            controls.DiverControls.Move.canceled += OnMoveCanceled;
        }

        private void OnDisable()
        {
            // Desuscripción obligatoria para evitar errores
            controls.DiverControls.Move.performed -= OnMovePerformed;
            controls.DiverControls.Move.canceled -= OnMoveCanceled;

            controls.Disable();
        }

        private void FixedUpdate()
        {
            // Si el script está desactivado, no hacemos nada
            if (!enabled)
            {
                return;
            }

            ApplyGravity();
            UpdateMovement();
            EnforceSurfaceLimit();
        }

        #endregion

        #region Input Callbacks

        // Se ejecuta al mover el stick. Estos son diferentes respecto a los del barco porque el buceador se mueve en 2D (X e Y), mientras que el barco solo en X.
        private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        // Se ejecuta al soltar el stick
        private void OnMoveCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            moveInput = Vector2.zero;
        }

        #endregion

        #region Movement Logic

        private void ApplyGravity()
        {
            // Aplicar gravedad suave constante hacia abajo
            rb.AddForce(Vector3.down * underwaterGravity, ForceMode.Force);
        }

        private void UpdateMovement()
        {
            // Calcular velocidad deseada
            Vector2 targetVelocity = moveInput * swimSpeed;

            //  Decidir si aceleramos o frenamos 
            float rate;
            if (moveInput.magnitude > 0.01f)
            {
                rate = acceleration;
            }
            else
            {
                rate = waterDrag;
            }

            //  Suavizar el cambio de velocidad con MoveTowards
            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            // Aplicar restricciones especiales (Barco)
            currentVelocity = ApplyHemisphereConstraint(currentVelocity);

            //  Mover el Rigidbody
            Vector3 movement = new Vector3(currentVelocity.x, currentVelocity.y, 0f) * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        private Vector2 ApplyHemisphereConstraint(Vector2 velocity)
        {
            // Si no hay barco asignado, no limitamos nada
            if (boatTransform == null)
            {
                return velocity;
            }

            // Lógica: Si intentamos subir (Y > 0)
            if (velocity.y > 0)
            {
                float distanceToBoat = Mathf.Abs(transform.position.y - boatTransform.position.y);

                // Si estamos demasiado cerca del barco, prohibimos subir más
                if (distanceToBoat < minDepthFromBoat)
                {
                    // Forzamos la velocidad Y a 0 (o menos)
                    velocity.y = Mathf.Min(velocity.y, 0f);
                }
            }

            return velocity;
        }

        private void EnforceSurfaceLimit()
        {
            // Lógica: Si salimos del agua
            if (transform.position.y > waterSurfaceY)
            {
                //  Teletransportar de vuelta a la superficie
                Vector3 pos = transform.position;
                pos.y = waterSurfaceY;
                transform.position = pos;
                rb.position = pos;

                if(moveInput.y > 0f)
                {
                    currentVelocity.y = 0f;// Cancelamos cualquier intento de seguir subiendo si el jugador sigue pulsando hacia arriba, para evitar que se quede atascado intentando subir sin poder porque ya está en la superficie.
                    Vector3 vel = rb.linearVelocity;
                    vel.y = 0f;
                    rb.linearVelocity = vel;
                }
                else if(moveInput.y < 0f)
                {
                    // Si el jugador está intentando bajar, permitimos que siga bajando aunque esté en la superficie, para que pueda volver a sumergirse sin problemas.
                    
                        Debug.Log("[DiverMovement] En superficie - Permitiendo movimiento hacia abajo");
                    
                }
                else            
                {
                    // Solo cancelar velocidad hacia arriba si la hay
                    Vector3 vel = rb.linearVelocity;
                    if (vel.y > 0)
                    {
                        vel.y = 0f;
                        rb.linearVelocity = vel;
                    }
                }

            }
        }

        #endregion

        #region Aplicaciones Publicas

        // Detiene el movimiento del buceador en seco
        public void Stop()
        {
            currentVelocity = Vector2.zero;
            moveInput = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }

        // Posiciona el buceador y asegura que esté bajo el agua
        public void SetPosition(Vector3 position)
        {
            position.z = 0f;
            // Mathf.Min asegura que nunca spawnee por encima de la superficie
            position.y = Mathf.Min(position.y, waterSurfaceY);

            transform.position = position;
            rb.position = position;
            Stop();
        }

        public void SetBoatReference(Transform boat)
        {
            boatTransform = boat;
        }

        public Vector2 GetCurrentVelocity()
        {
            return currentVelocity;
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            // Si no estamos jugando, no dibujamos la velocidad porque sería 0
            if (!Application.isPlaying)
            {
                return;
            }

            // Flecha de Velocidad = Cyan
            Gizmos.color = Color.cyan;
            Vector3 vel3D = new Vector3(currentVelocity.x, currentVelocity.y, 0f);
            Gizmos.DrawRay(transform.position, vel3D); // Sirve para ver hacia dónde y qué tan rápido se está intentando mover el personaje en ese instante.

            // Línea de Superficie del agua
            Gizmos.color = Color.blue;
            float xPos = transform.position.x;
            Gizmos.DrawLine( new Vector3(xPos - 5f, waterSurfaceY, 0f), new Vector3(xPos + 5f, waterSurfaceY, 0f)); // Dibuja una línea de 10 metros de ancho que sigue al jugador horizontalmente pero se mantiene fija en la altura del agua. Indica dónde está el límite para salir a la superficie.

            //  Línea de Límite del Barco = Amarilla
            if (boatTransform != null)
            {
                Gizmos.color = Color.yellow;
                float minY = boatTransform.position.y - minDepthFromBoat;
                Gizmos.DrawLine(new Vector3(xPos - 3f, minY, 0f),new Vector3(xPos + 3f, minY, 0f)); // Indica la altura mínima a la que el buceador puede acercarse al barco. Si intenta subir por encima de esta línea, se le bloqueará el movimiento hacia arriba para evitar que se meta dentro del barco o salga a la superficie demasiado cerca de él.
            }
        }

        #endregion
    }
}