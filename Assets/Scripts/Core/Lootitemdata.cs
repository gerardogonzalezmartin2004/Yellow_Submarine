using UnityEngine;

namespace AbyssalReach.Data
{
         
    [CreateAssetMenu(fileName = "New Loot Item", menuName = "AbyssalReach/Loot Item", order = 1)] // Para crear un nuevo Loot Item: Assets > Create > AbyssalReach > Loot Item
    public class LootItemData : ScriptableObject
    {
        // Este ScriptableObject representa los items que el buceador puede recoger.
        [Header("Basic Info")]
        [Tooltip("Nombre del item (ej: 'Cofre Dorado')")]
        public string itemName = "New Item";

        [Tooltip("Icono para mostrar en UI")]
        public Sprite icon;

        [Header("Properties")]
        [Tooltip("Valor en monedas de oro")]
        [Min(1)]
        public int value = 5;

        [Tooltip("Peso en kg (afecta la física del cable)")]
        [Min(0.1f)]
        public float weight = 1f;

        [Tooltip("Rareza del item")]
        public ItemRarity rarity = ItemRarity.Common;

        [Header("Visual")]
        [Tooltip("Color del aura según rareza (calculado automáticamente)")]
        [SerializeField] private Color auraColor;

        [Header("Optional")]
        [TextArea(2, 4)]
        [Tooltip("Descripción del item")]
        public string description;

        #region Auto-Configuration

        private void OnValidate()
        {
            // Auto asignar el  color de aura según rareza
            auraColor = GetAuraColorForRarity(rarity);

            // Auto ajusta el  valor sugerido según rareza
            if (value == 0)
            {
                value = GetSuggestedValueForRarity(rarity);
            }
        }

        #endregion

        #region Helper Methods

       
     
        // Obtiene el color de aura según la rareza
        public Color GetAuraColor()
        {
            return GetAuraColorForRarity(rarity);
        }

        private Color GetAuraColorForRarity(ItemRarity itemRarity)
        {
            switch (itemRarity)
            {
                case ItemRarity.Common:
                    return new Color(0.7f, 0.7f, 0.7f); // Gris
                case ItemRarity.Rare:
                    return new Color(0.2f, 0.6f, 1f);   // Azul
                case ItemRarity.Epic:
                    return new Color(0.6f, 0.2f, 1f);   // Morado
                case ItemRarity.Legendary:
                    return new Color(1f, 0.8f, 0.2f);   // Dorado
                default:
                    return Color.white;
            }
        }

        private int GetSuggestedValueForRarity(ItemRarity itemRarity)
        {
            switch (itemRarity)
            {
                case ItemRarity.Common:
                    return 5;
                case ItemRarity.Rare:
                    return 10;
                case ItemRarity.Epic:
                    return 20;
                case ItemRarity.Legendary:
                    return 50;
                default:
                    return 1;
            }
        }

        #endregion
    }

  
    public enum ItemRarity // Rarezas disponibles para los items, con colores y valores sugeridos asociados
    {
        Common,      // Gris - Valor: ~5
        Rare,        // Azul - Valor: ~10
        Epic,        // Morado - Valor: ~20
        Legendary    // Dorado - Valor: ~50
    }
}