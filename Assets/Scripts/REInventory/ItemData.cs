using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data", order = 1)]
public class ItemData : ScriptableObject
{
    [Header("Identificación")]
    public string itemName = "Nuevo Item";

    [Header("Tamaño en el Grid")]
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;

    [Header("Visual")]
    public Sprite itemIcon;

    [Header("Rareza")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Propiedades")]
    [Min(0)] public int value = 5;
    [Min(0)] public float weight = 1f;

    [Header("Descripción")]
    [TextArea(2, 4)]
    public string description;

    // Rareza con sus implicaciones de color y valor sugerido
    public enum ItemRarity
    {
        Common,    
        Rare,     
        Epic,      
        Legendary  
    }

    // Devuelve el color del aura según la rareza.
    // BagVisualizer, ShopUI o cualquier UI puede llamar esto para colorear el borde.
    public Color GetAuraColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.7f, 0.7f, 0.7f);
            case ItemRarity.Rare: return new Color(0.2f, 0.6f, 1f);
            case ItemRarity.Epic: return new Color(0.6f, 0.2f, 1f);
            case ItemRarity.Legendary: return new Color(1f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }

    // Rellena value con el valor sugerido según rareza si está a 0.
    // Se llama automáticamente al cambiar algo en el Inspector.
    private void OnValidate()
    {
        if (value == 0)
            value = GetSuggestedValue();

        if (string.IsNullOrEmpty(itemName))
            itemName = name;
    }

    private int GetSuggestedValue()
    {
        switch (rarity)
        {
            case ItemRarity.Common: return 5;
            case ItemRarity.Rare: return 10;
            case ItemRarity.Epic: return 20;
            case ItemRarity.Legendary: return 50;
            default: return 5;
        }
    }

    public int GetArea() => width * height;
}