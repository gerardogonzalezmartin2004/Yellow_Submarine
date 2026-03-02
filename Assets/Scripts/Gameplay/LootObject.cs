using AbyssalReach.Data;
using UnityEngine;

namespace AbyssalReach.Gameplay
{
    
   
    [RequireComponent(typeof(Rigidbody2D))]
    public class LootObject : MonoBehaviour
    {
        // Gestionar el estado, físicas y degradación de valor de los tesoros
        [Header("Item Data")]
        [Tooltip("ScriptableObject que define qué es este objeto (valor, peso, rareza)")]
        [SerializeField] private LootItemData itemData;

        [Header("Damage Settings")]
        [Tooltip("Cuánto valor pierde por cada golpe fuerte")]
        [SerializeField] private int valueLossPerHit = 1;

        [Tooltip("Velocidad mínima para considerar un golpe (m/s)")]
        [SerializeField] private float damageThreshold = 3f;

        [Header("Water Physics")]
        [Tooltip("Drag bajo el agua para que no caiga como una piedra")]
        [SerializeField] private float waterDrag = 2f;

        [Tooltip("Drag angular bajo el agua")]
        [SerializeField] private float waterAngularDrag = 1f;

        [Header("Visual Feedback")]
        [Tooltip("Renderer del objeto (para cambiar color al dañarse)")]
        [SerializeField] private SpriteRenderer objectRenderer; // Cambiado a SpriteRenderer

        [Tooltip("Color cuando está intacto")]
        [SerializeField] private Color intactColor = Color.white;

        [Tooltip("Color cuando está muy dañado")]
        [SerializeField] private Color damagedColor = new Color(0.8f, 0.3f, 0.3f);

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Rigidbody2D rb; // Cambiado a Rigidbody2D
        private int currentValue;
        private int baseValue;
        private bool isGrabbed = false;

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // Configurar físicas bajo el agua para 2D
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;

            // IMPORTANTE: En 2D no existe "useGravity = false", se pone gravityScale a 0
            rb.gravityScale = 0f;

            if (itemData == null)
            {
                Debug.LogError("[LootObject] Item Data no asignado en " + gameObject.name);
            }
        }

        private void Start()
        {
            // Inicializar el valor actual igual al del ScriptableObject
            if (itemData != null)
            {
                baseValue = itemData.value;
                currentValue = baseValue;
            }

            // Cachear el renderer si no está asignado
            if (objectRenderer == null)
            {
                objectRenderer = GetComponent<SpriteRenderer>();
            }

            UpdateVisualState();
        }

        private void FixedUpdate()
        {
            // Solo aplicamos la gravedad artificial si no está enganchado
            if (!isGrabbed)
            {
                // Usamos Vector2 y ForceMode2D para 2D
                rb.AddForce(Vector2.down * 2f, ForceMode2D.Force);
            }
        }

        #endregion

        #region Collision Damage

        // IMPORTANTE: Cambiado a OnCollisionEnter2D
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Solo recibe daño si está siendo arrastrado por el buzo
            if (!isGrabbed || IsDestroyed())
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed >= damageThreshold)
            {
                ApplyDamage();

                if (showDebug)
                {
                    Debug.Log("[LootObject] ¡Golpe! Velocidad: " + impactSpeed + " | Valor restante: " + currentValue);
                }
            }
        }

        private void ApplyDamage()
        {
            // Se reduce el valor actual, pero nunca por debajo de 0
            currentValue = Mathf.Max(0, currentValue - valueLossPerHit);

            // Actualizar feedback visual
            UpdateVisualState();
        }

        #endregion

        #region Visual Feedback

        private void UpdateVisualState()
        {
            if (objectRenderer == null || itemData == null)
            {
                return;
            }

            // Calcular el porcentaje de valor restante
            float healthPct = (float)currentValue / (float)baseValue;

            // Interpolar el color entre intacto y dañado
            Color targetColor = Color.Lerp(damagedColor, intactColor, healthPct);

            // Aplicar el color multiplicado con el color de rareza
            Color rarityColor = itemData.GetAuraColor();

            // IMPORTANTE: En 2D con SpriteRenderer se cambia directamente el .color, no el material
            objectRenderer.color = targetColor * rarityColor;
        }

        #endregion

        #region API Pública

        // Marca el objeto como agarrado para activar el sistema de daño
        public void SetGrabbed(bool grabbed)
        {
            isGrabbed = grabbed;
        }
        public bool IsGrabbed()
        {
            return isGrabbed;
        }

        // Devuelve el valor actual 
        public int GetCurrentValue()
        {
            return currentValue;
        }

        // Devuelve el valor base del ScriptableObject
        public int GetBaseValue()
        {
            return baseValue;
        }

        // Devuelve el LootItemData asociado
        public LootItemData GetItemData()
        {
            return itemData;
        }

        // Devuelve el Rigidbody2D para el sistema de joints
        public Rigidbody2D GetRigidbody()
        {
            return rb;
        }

        // Verifica si el objeto ha perdido todo su valor
        public bool IsDestroyed()
        {
            if (currentValue <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            if (!showDebug)
            {
                return;
            }

            // Dibujar esfera que indica el valor restante
            if (itemData != null)
            {
                // Calcular el porcentaje de valor restante para el color
                float healthPct = 1f;
                if (Application.isPlaying && baseValue > 0)
                {
                    healthPct = (float)currentValue / (float)baseValue;
                }

                Gizmos.color = Color.Lerp(Color.red, Color.green, healthPct);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }

        #endregion
    }
}