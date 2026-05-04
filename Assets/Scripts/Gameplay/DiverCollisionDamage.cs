using UnityEngine;

public class DiverCollisionDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Oro que pierde cada item por golpe")]
    [SerializeField] private int damagePerHit = 1;

    [Tooltip("Tiempo mínimo entre golpes")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Header("Layer Filter")]
    [Tooltip("Capas que causan daño")]
    [SerializeField] private LayerMask damageLayers = ~0;

    private float lastHitTime = -999f;

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (Time.time - lastHitTime < hitCooldown) return;
        if (((1 << col.gameObject.layer) & damageLayers) == 0) return;
        if (ItemDamageTracker.Instance == null) return;

        lastHitTime = Time.time;
        ItemDamageTracker.Instance.ApplyDamageToAll(damagePerHit);
    }
}