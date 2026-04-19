using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DiverMovement : MonoBehaviour
    {
        // Controla el movimiento del buceador con física de agua.

        [Header("Movement Settings")]
        [Tooltip("Velocidad máxima de nado")]
        public float swimSpeed = 5f;

        [Tooltip("Aceleración al empezar a nadar")]
        [SerializeField] private float acceleration = 8f;

        [Tooltip("Desaceleración al soltar los controles (inercia del agua)")]
        [SerializeField] private float waterDrag = 12f;

        [Tooltip("Fuerza máxima para no pelear bruscamente contra la cuerda")]
        [SerializeField] private float maxSwimForce = 60f;

        [Tooltip("Distancia a la que empieza a frenar suavemente cerca de los límites")]
        [SerializeField] private float softBrakeDistance = 2f;

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
        [SerializeField] private ropeVerlet rope;
        public bool emergencyAscent = false;

        // CAMBIO A 2D: rb ahora es Rigidbody2D
        [SerializeField] private Rigidbody2D rb;
        private AbyssalReachControls controls;

        private Vector2 moveInput = Vector2.zero;
        private Vector2 currentVelocity = Vector2.zero;

        #region Unity ciclo de vida

        private void Awake()
        {

            rb = GetComponent<Rigidbody2D>();

            // Configurar Rigidbody2D
            rb.gravityScale = 0f;
            rb.linearDamping = rbDrag;


            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

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
            if (!enabled) return;

            ApplyGravity();

            if (!emergencyAscent)
            {
                UpdateMovement();
            }
            else
            {

                Debug.DrawRay(transform.position, new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f).normalized * 6f, Color.green, 0.15f);
            }


            EnforcePhysicsLimits();
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
            if (!emergencyAscent)
            {
                rb.AddForce(Vector2.down * underwaterGravity, ForceMode2D.Force);
            }
            else
            {
                rb.AddForce(rope.GetTensionDirection() * underwaterGravity, ForceMode2D.Force);
            }
        }

        private void UpdateMovement()
        {
            // 1. Calculamos la velocidad a la que queremos ir según el joystick
            Vector2 targetVelocity = moveInput * swimSpeed;

            // --- NUEVO: FRENADO SUAVE (AMORTIGUADOR VERTICAL) ---
            if (targetVelocity.y > 0) // Solo si estamos nadando hacia arriba
            {
                // Calculamos a qué distancia estamos del límite más cercano
                float distToSurface = waterSurfaceY - rb.position.y;
                float distToBoat = boatTransform != null ? Mathf.Abs(rb.position.y - boatTransform.position.y) - minDepthFromBoat : float.MaxValue;

                float closestLimit = Mathf.Min(distToSurface, distToBoat);

                // Si entramos en la zona de frenado (por defecto 2 metros antes del límite)
                if (closestLimit < softBrakeDistance && closestLimit > 0)
                {
                    // Multiplicador que va de 1 (lejos) a 0 (tocando el límite)
                    float brakeMultiplier = closestLimit / softBrakeDistance;

                    // Suavizamos la intención de subir del jugador
                    targetVelocity.y *= brakeMultiplier;
                }
            }
            // ----------------------------------------------------

            // 2. Diferencia entre la velocidad física actual y la que queremos
            Vector2 velocityDifference = targetVelocity - rb.linearVelocity;

            // 3. ¿Aceleramos o aplicamos fricción de agua?
            float rate = (moveInput.magnitude > 0.01f) ? acceleration : waterDrag;

            // 4. Fuerza bruta calculada
            Vector2 movementForce = velocityDifference * rate * rb.mass;

            // --- NUEVO: ELASTICIDAD CONTRA LA CUERDA ---
            // Limitamos la fuerza máxima que puede hacer el buzo. 
            // Si la cuerda tira hacia atrás, el buzo no podrá aplicar fuerza infinita para frenar en seco, cediendo de forma natural.
            movementForce = Vector2.ClampMagnitude(movementForce, maxSwimForce * rb.mass);

            // Aplicamos la fuerza final
            rb.AddForce(movementForce, ForceMode2D.Force);

            // 5. Mantenemos el seguro de vida por si una fuerza externa lo saca del agua
            EnforcePhysicsLimits();
        }

        private void EnforcePhysicsLimits()
        {
            // --- NUEVO: Si hay emergencia, apagamos los campos magnéticos para que la cuerda nos suba ---
            if (emergencyAscent)
            {
                // Solo mantenemos el seguro de fallos absoluto de la superficie
                if (rb.position.y > waterSurfaceY + 0.1f)
                {
                    rb.position = new Vector2(rb.position.x, waterSurfaceY);
                    if (rb.linearVelocity.y > 0)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                    }
                }
                return; // Cortamos la ejecución aquí
            }

            // --- CAMPO DE REPULSIÓN PROGRESIVO ---

            // 1. Campo magnético del Barco
            if (boatTransform != null && rb.linearVelocity.y > 0)
            {
                float distanceToBoat = Mathf.Abs(rb.position.y - boatTransform.position.y);

                if (distanceToBoat < (minDepthFromBoat + softBrakeDistance))
                {
                    float intensity = 1f - ((distanceToBoat - minDepthFromBoat) / softBrakeDistance);
                    float repulsionForce = intensity * maxSwimForce * rb.mass * 2f;

                    rb.AddForce(Vector2.down * repulsionForce, ForceMode2D.Force);
                }
            }

            // 2. Campo magnético de la Superficie del Agua
            float distToSurface = waterSurfaceY - rb.position.y;
            if (distToSurface < softBrakeDistance && rb.linearVelocity.y > 0)
            {
                float intensity = 1f - (distToSurface / softBrakeDistance);
                float repulsionForce = intensity * maxSwimForce * rb.mass * 2f;

                rb.AddForce(Vector2.down * repulsionForce, ForceMode2D.Force);
            }

            // 3. Seguro de fallos normal
            if (rb.position.y > waterSurfaceY + 0.1f)
            {
                rb.position = new Vector2(rb.position.x, waterSurfaceY);
                if (rb.linearVelocity.y > 0)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                }
            }
        }

        #endregion

        #region Aplicaciones Publicas

        public void Stop()
        {
            moveInput = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        public void EnterEmergencyAscent()
        {
            emergencyAscent = true;
            moveInput = Vector2.zero;
            rb.linearDamping = 0.4f;
            Debug.Log("[DiverMovement] Emergencia activada - solo física de cuerda");
        }

        public void ExitEmergencyAscent()
        {
            emergencyAscent = false;
            rb.mass = 10f;
            rb.linearDamping = rbDrag;
        }

        public void SetPosition(Vector2 position)
        {
            position.y = Mathf.Min(position.y, waterSurfaceY);
            transform.position = new Vector3(position.x, position.y, 0f);
            rb.position = position;
            Stop();
        }

        public void SetBoatReference(Transform boat)
        {
            boatTransform = boat;
        }

        public Vector2 GetCurrentVelocity()
        {
            // Ahora devolvemos la velocidad real del objeto físico
            return rb != null ? rb.linearVelocity : Vector2.zero;
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
            Gizmos.DrawLine(new Vector3(xPos - 5f, waterSurfaceY, 0f), new Vector3(xPos + 5f, waterSurfaceY, 0f)); // Dibuja una línea de 10 metros de ancho que sigue al jugador horizontalmente pero se mantiene fija en la altura del agua. Indica dónde está el límite para salir a la superficie.

            //  Línea de Límite del Barco = Amarilla
            if (boatTransform != null)
            {
                Gizmos.color = Color.yellow;
                float minY = boatTransform.position.y - minDepthFromBoat;
                Gizmos.DrawLine(new Vector3(xPos - 3f, minY, 0f), new Vector3(xPos + 3f, minY, 0f)); // Indica la altura mínima a la que el buceador puede acercarse al barco. Si intenta subir por encima de esta línea, se le bloqueará el movimiento hacia arriba para evitar que se meta dentro del barco o salga a la superficie demasiado cerca de él.
            }
        }

        #endregion
    }
}