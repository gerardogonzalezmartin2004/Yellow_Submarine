using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DiverInventory
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    [SerializeField] private float maxWeight = 20f;

    public bool TryAddItem(ItemData item, out string error)
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

    public List<ItemData> GetItems()
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