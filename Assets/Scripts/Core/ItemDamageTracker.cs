using System.Collections.Generic;
using UnityEngine;

public class ItemDamageTracker : MonoBehaviour
{
    private static ItemDamageTracker instance;
    public static ItemDamageTracker Instance => instance;

    private readonly Dictionary<ItemData, int> damages = new Dictionary<ItemData, int>();

    public delegate void DamageApplied(int goldLostThisHit);
    public static event DamageApplied OnDamageApplied;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public int GetRuntimeValue(ItemData data)
    {
        if (data == null) return 0;
        int damageDone = damages.ContainsKey(data) ? damages[data] : 0;
        return Mathf.Max(0, data.value - damageDone);
    }

    public void ApplyDamageToAll(int damagePerItem)
    {
        if (AbyssalReach.Core.InventoryManager.Instance == null) return;

        List<ItemData> items = AbyssalReach.Core.InventoryManager.Instance
     .GetDiverInventory().GetItemDatas();
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

    public void ClearDamages()
    {
        damages.Clear();
    }
}