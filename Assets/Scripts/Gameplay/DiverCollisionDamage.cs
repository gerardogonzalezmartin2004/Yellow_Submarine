using UnityEngine;

// Poner en: Diver y BagObject
// Detecta colisiones 2D y aplica daño de valor a los items del inventario
public class DiverCollisionDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Oro que pierde cada item por golpe")]
    [SerializeField] private int damagePerHit = 1;

    [Tooltip("Tiempo mínimo entre golpes (evita spam de colisión)")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Layer Filter")]
    [Tooltip("Capas que causan daño. Por defecto todo.")]
    [SerializeField] private LayerMask damageLayers ;

    private float lastHitTime = -999f;

    private void OnCollisionEnter2D(Collision2D col)
    {
        // Cooldown entre golpes
        if (Time.time - lastHitTime < hitCooldown) return;

        // Filtro de capas
        if (((1 << col.gameObject.layer) & damageLayers) == 0) return;

        // Sin tracker o sin items, salir
        if (ItemDamageTracker.Instance == null) return;

        lastHitTime = Time.time;
        ItemDamageTracker.Instance.ApplyDamageToAll(damagePerHit);

        Debug.Log("[DiverCollisionDamage] Golpe de " + gameObject.name
                  + " con " + col.gameObject.name);
    }
}