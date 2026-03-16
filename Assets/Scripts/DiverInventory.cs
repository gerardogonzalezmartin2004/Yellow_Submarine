using System.Collections.Generic;
using UnityEngine;
using AbyssalReach.Data;

[System.Serializable]
public class DiverInventory
{
    [SerializeField] private List<LootItemData> items = new List<LootItemData>();

    [SerializeField] private float maxWeight = 20f;

    public bool TryAddItem(LootItemData item, out string error)
    {
        error = "";

        float newWeight = GetCurrentWeight() + item.weight;

        if (newWeight > maxWeight)
        {
            error = "Peso máximo alcanzado";
            return false;
        }

        items.Add(item);
        return true;
    }

    public float GetCurrentWeight()
    {
        float weight = 0f;

        foreach (var item in items)
        {
            weight += item.weight;
        }

        return weight;
    }

    public float GetMaxWeight()
    {
        return maxWeight;
    }

    public List<LootItemData> GetItems()
    {
        return items;
    }

    public int GetItemCount()
    {
        return items.Count;
    }

    public void Clear()
    {
        items.Clear();
    }
}