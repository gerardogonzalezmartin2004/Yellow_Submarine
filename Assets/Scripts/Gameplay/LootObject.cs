using AbyssalReach.Data;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace AbyssalReach.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class LootObject : MonoBehaviour
    {
        //Gestionar el estado, físicas y degradación de valor de los tesoros
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
        [SerializeField] private Renderer objectRenderer;

        [Tooltip("Color cuando está intacto")]
        [SerializeField] private Color intactColor = Color.white;

        [Tooltip("Color cuando está muy dañado")]
        [SerializeField] private Color damagedColor = new Color(0.8f, 0.3f, 0.3f);

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        private Rigidbody rb;
        private int currentValue;
        private int baseValue;
        private bool isGrabbed = false;

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
           

            // Configurar físicas bajo el agua
            rb.linearDamping = waterDrag;
            rb.angularDamping = waterAngularDrag;
            rb.useGravity = false; // La gravedad la aplicamos manualmente en el agua

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
                objectRenderer = GetComponent<Renderer>();
            }

            UpdateVisualState();
        }

        private void FixedUpdate()
        {
            // Solo aplicamos la gravedad artificial si no está enganchado
            if (!isGrabbed)
            {
                rb.AddForce(Vector3.down * 2f, ForceMode.Force);
            }
        }

        #endregion

        #region Collision Damage

        private void OnCollisionEnter(Collision collision)
        {
            // Solo recibe daño si está siendo arrastrado por el buzo
            if (!isGrabbed || IsDestroyed()) return;

            float impactSpeed = collision.relativeVelocity.magnitude;

            if (impactSpeed >= damageThreshold)
            {
                ApplyDamage();
               
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
            objectRenderer.material.color = targetColor * rarityColor;
        }

        #endregion

        #region API Pública

        // Marca el objeto como agarrado para activar el sistema de daño
        public void SetGrabbed(bool grabbed)
        {
            isGrabbed = grabbed;

         
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

        // Devuelve el Rigidbody para el sistema de joints
        public Rigidbody GetRigidbody()
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