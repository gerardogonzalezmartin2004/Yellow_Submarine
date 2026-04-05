using UnityEngine;

namespace AbyssalReach.UI
{
   
    public class SmartPrompt : MonoBehaviour
    {
        public enum TargetType
        {
            DiverOnly,
            BoatOnly,
            Both
        }

        [Header("QUIÉN PUEDE INTERACTUAR")]
        [SerializeField] private TargetType targetType = TargetType.DiverOnly;

        [Header("SPRITES")]
        [SerializeField] private Sprite farSprite;  
        [SerializeField] private Sprite nearSprite; 

        [Header("RANGOS")]
        [SerializeField] private float farRadius = 5f;
        [SerializeField] private float nearRadius = 2f;

        [Header("VISUALES")]
        [SerializeField] private Vector3 iconScale = new Vector3(3, 3, 1); 
        [SerializeField] private int sortingOrder = 100;

        private SpriteRenderer spriteRenderer;
        private GameObject diver;
        private GameObject boat;

        void Awake()
        {
            // Setup SpriteRenderer
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.enabled = false;
            transform.localScale = iconScale;
        }

        void Update()
        {
            // Buscar objetos necesarios
            FindTargets();

            // Calcular distancia al objetivo más cercano
            float closestDistance = float.MaxValue;
            bool hasValidTarget = false;

            if (ShouldCheckDiver() && diver != null)
            {
                float dist = Vector2.Distance(transform.position, diver.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    hasValidTarget = true;
                }
            }

            if (ShouldCheckBoat() && boat != null)
            {
                float dist = Vector2.Distance(transform.position, boat.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    hasValidTarget = true;
                }
            }

            // Actualizar estado visual
            if (!hasValidTarget || closestDistance > farRadius)
            {
              
                spriteRenderer.enabled = false;
            }
            else if (closestDistance > nearRadius)
            {
                
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = farSprite;
            }
            else
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sprite = nearSprite;
            }
        }

        private void FindTargets()
        {
            if (ShouldCheckDiver() && diver == null)
            {
                diver = GameObject.Find("Diver");
            }

            if (ShouldCheckBoat() && boat == null)
            {
                boat = GameObject.Find("Boat");
            }
        }

        private bool ShouldCheckDiver()
        {
            return targetType == TargetType.DiverOnly || targetType == TargetType.Both;
        }

        private bool ShouldCheckBoat()
        {
            return targetType == TargetType.BoatOnly || targetType == TargetType.Both;
        }

        void OnDrawGizmosSelected()
        {
            // Color según tipo
            Color gizmoColor = targetType == TargetType.DiverOnly ? Color.cyan :
                               targetType == TargetType.BoatOnly ? Color.blue :
                               Color.magenta;

            // Radio exterior
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, farRadius);

            // Radio interior
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, nearRadius);
        }
    }
}