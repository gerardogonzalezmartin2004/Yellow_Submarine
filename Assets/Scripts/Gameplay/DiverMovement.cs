using UnityEngine;
using AbyssalReach.Core;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class DiverMovement : MonoBehaviour
    {

        [Header("Movement Settings")]
        [Tooltip("Velocidad máxima de nado")]
        public float swimSpeed = 5f;

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
        [SerializeField] private ropeVerlet rope; 
        public bool emergencyAscent = false;

        [SerializeField] private Rigidbody2D rb;
        private AbyssalReachControls controls;

        private Vector2 moveInput = Vector2.zero;
        private Vector2 currentVelocity = Vector2.zero;

        #region Unity ciclo de vida

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            
            rb.gravityScale = 0f; 
            rb.linearDamping = rbDrag; 

           
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            controls = new AbyssalReachControls();
        }

        private void OnEnable()
        {
            controls.Enable();

          
            controls.DiverControls.Move.performed += OnMovePerformed;
            controls.DiverControls.Move.canceled += OnMoveCanceled;
        }

        private void OnDisable()
        {
           
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
            

            EnforceSurfaceLimit();
        }

        #endregion

        #region Input Callbacks

        private void OnMovePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

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
            Vector2 targetVelocity = moveInput * swimSpeed;

            float rate;
            if (moveInput.magnitude > 0.01f)
            {
                rate = acceleration;
            }
            else
            {
                rate = waterDrag;
            }

            currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            currentVelocity = ApplyHemisphereConstraint(currentVelocity);

            Vector2 movement = currentVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + movement);
        }

        private Vector2 ApplyHemisphereConstraint(Vector2 velocity)
        {
            if (boatTransform == null)
            {
                return velocity;
            }

            if (velocity.y > 0)
            {
                float distanceToBoat = Mathf.Abs(transform.position.y - boatTransform.position.y);

                if (distanceToBoat < minDepthFromBoat)
                {
                    velocity.y = Mathf.Min(velocity.y, 0f);
                }
            }

            return velocity;
        }

        private void EnforceSurfaceLimit()
        {
            if (transform.position.y > waterSurfaceY)
            {
                Vector2 pos = transform.position; 
                pos.y = waterSurfaceY;
                transform.position = pos; 
                rb.position = pos;

                if (moveInput.y > 0f)
                {
                    currentVelocity.y = 0f;
                    Vector2 vel = rb.linearVelocity; 
                    vel.y = 0f;
                    rb.linearVelocity = vel;
                }
                else if (moveInput.y < 0f)
                {
                    
                    Debug.Log("[DiverMovement] En superficie - Permitiendo movimiento hacia abajo");
                }
                else
                {
                   
                    Vector2 vel = rb.linearVelocity; 
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

        
        public void Stop()
        {
            currentVelocity = Vector2.zero;
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
            currentVelocity = Vector2.zero;
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
            return currentVelocity;
        }
        public void UpgradeSwimSpeed(float amount)
        {
            swimSpeed += amount;
        }
        public Vector3 GetPosition()
        {
            return transform.position;
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 vel3D = new Vector3(currentVelocity.x, currentVelocity.y, 0f);
            Gizmos.DrawRay(transform.position, vel3D);

            Gizmos.color = Color.blue;
            float xPos = transform.position.x;
            Gizmos.DrawLine(new Vector3(xPos - 5f, waterSurfaceY, 0f), new Vector3(xPos + 5f, waterSurfaceY, 0f));

          
            if (boatTransform != null)
            {
                Gizmos.color = Color.yellow;
                float minY = boatTransform.position.y - minDepthFromBoat;
                Gizmos.DrawLine(new Vector3(xPos - 3f, minY, 0f), new Vector3(xPos + 3f, minY, 0f)); 
            }
        }

        #endregion
    }
}