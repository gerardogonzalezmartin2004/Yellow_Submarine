using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DiverInventory
{
    // Guardamos el ItemData y el ameObject del mundo 
    // Así al descartar podemos reactivarlo sin instanciar nada nuevo.
    [System.Serializable]
    public class DiverItem
    {
        public ItemData data;
        public GameObject worldObject; 

        public DiverItem(ItemData data, GameObject worldObject)
        {
            this.data = data;
            this.worldObject = worldObject;
        }
    }

    [SerializeField] private List<DiverItem> items = new List<DiverItem>();
    [SerializeField] private float maxWeight = 20f;

    public bool TryAddItem(ItemData item, GameObject worldObject, out string error)
    {
        error = "";

        float newWeight = GetCurrentWeight() + item.weight;
        if (newWeight > maxWeight)
        {
            error = "Peso máximo alcanzado";
            return false;
        }

        items.Add(new DiverItem(item, worldObject));
        return true;
    }

    public float GetCurrentWeight()
    {
        float weight = 0f;
        foreach (var item in items)
            weight += item.data.weight;
        return weight;
    }

    public float GetMaxWeight() => maxWeight;

    // Devuelve solo los ItemData 
    public List<ItemData> GetItemDatas()
    {
        var result = new List<ItemData>();
        foreach (var item in items)
            result.Add(item.data);
        return result;
    }

    // Devuelve la lista completa con referencias a world objects.
    public List<DiverItem> GetItems() => items;

    public int GetItemCount() => items.Count;

    public void Clear()
    {
        items.Clear();
    }
}