using System.Collections.Generic;
using UnityEngine;

public class ItemDamageTracker : MonoBehaviour
{
    private static ItemDamageTracker instance;
    public static ItemDamageTracker Instance => instance;

    // Clave: el ScriptableObject del item. Valor: daño acumulado en runtime.
    // Así funciona aunque el item esté en el diver O en el barco.
    private readonly Dictionary<ItemData, int> damages = new Dictionary<ItemData, int>();

    // Ahora solo emite el oro perdido en ESTE golpe, no el acumulado
    public delegate void DamageApplied(int goldLostThisHit);
    public static event DamageApplied OnDamageApplied;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    // Devuelve el valor runtime de un ItemData (base - daño acumulado)
    public int GetRuntimeValue(ItemData data)
    {
        if (data == null) return 0;
        int damageDone = damages.ContainsKey(data) ? damages[data] : 0;
        return Mathf.Max(0, data.value - damageDone);
    }

    // Aplica daño a todos los items del diver y dispara evento con el daño de ESTE golpe
    public void ApplyDamageToAll(int damagePerItem)
    {
        if (AbyssalReach.Core.InventoryManager.Instance == null) return;

        List<ItemData> items = AbyssalReach.Core.InventoryManager.Instance
                                            .GetDiverInventory().GetItems();
        if (items == null || items.Count == 0) return;

        int goldLostThisHit = 0;

        foreach (ItemData item in items)
        {
            if (item == null) continue;

            int damageSoFar = damages.ContainsKey(item) ? damages[item] : 0;
            int remaining = item.value - damageSoFar;

            if (remaining <= 0) continue;

            int actual = Mathf.Min(damagePerItem, remaining);
            damages[item] = damageSoFar + actual;
            goldLostThisHit += actual;
        }

        if (goldLostThisHit > 0)
            OnDamageApplied?.Invoke(goldLostThisHit);
    }

    // Llamar cuando se vende todo o se resetea el juego
    public void ClearDamages()
    {
        damages.Clear();
    }
}