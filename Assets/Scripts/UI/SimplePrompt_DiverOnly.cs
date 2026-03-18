using UnityEngine;

namespace AbyssalReach.UI
{
    /// <summary>
    /// SCRIPT 1: Detecta SOLO al DIVER - Muestra 1 sprite cuando entra en rango
    /// SUPER SIMPLE - Solo lo básico
    /// </summary>
    public class SimplePrompt_DiverOnly : MonoBehaviour
    {
        [Header("CONFIG")]
        [SerializeField] private Sprite iconSprite; // El sprite a mostrar
        [SerializeField] private float detectionRadius = 5f; // Rango de detección
        [SerializeField] private Vector3 iconScale = new Vector3(3, 3, 1); // GRANDE por defecto

        private SpriteRenderer spriteRenderer;
        private GameObject diver;

        void Awake()
        {
            // Crear o obtener SpriteRenderer
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            // Configurar sprite
            spriteRenderer.sprite = iconSprite;
            spriteRenderer.sortingOrder = 100;
            spriteRenderer.enabled = false; // Oculto al inicio

            // Escala GRANDE
            transform.localScale = iconScale;
        }

        void Update()
        {
            // Buscar diver si no lo tenemos
            if (diver == null)
            {
                diver = GameObject.Find("Diver");
                if (diver == null) return;
            }

            // Calcular distancia
            float distance = Vector2.Distance(transform.position, diver.transform.position);

            // Mostrar/ocultar según distancia
            spriteRenderer.enabled = (distance <= detectionRadius);
        }

        // Dibujar gizmo en editor
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}