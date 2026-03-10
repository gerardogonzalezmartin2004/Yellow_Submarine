using UnityEngine;

namespace AbyssalReach.Data
{
    /// <summary>
    /// Define las formas que puede tener un item en el inventario grid
    /// Cada forma está predefinida para facilitar el diseño de items
    /// </summary>
    public enum ItemShape
    {
        Single_1x1,      // 1x1 - Moneda, anillo, gema pequeña
        Horizontal_2x1,  // 2x1 - Cuchillo, llave
        Vertical_1x2,    // 1x2 - Botella, vial
        Horizontal_3x1,  // 3x1 - Espada corta, rifle pequeño
        Vertical_1x3,    // 1x3 - Espada larga, arpón
        Square_2x2,      // 2x2 - Cofre pequeño, libro
        Square_3x3,      // 3x3 - Cofre grande, escudo
        LShape_2x2,      // Forma L (2x2) - Ancla pequeña
        TShape_3x2       // Forma T (3x2) - Ancla grande, tridente
    }

    [CreateAssetMenu(fileName = "New Loot Item", menuName = "AbyssalReach/Loot Item", order = 1)]
    public class LootItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Nombre del item (ej: 'Cofre Dorado')")]
        public string itemName = "New Item";

        [Tooltip("Icono para mostrar en UI")]
        public Sprite icon;

        [Header("Grid Properties")]
        [Tooltip("Forma que ocupa el item en el inventario grid")]
        public ItemShape shape = ItemShape.Single_1x1;

        [Header("Properties")]
        [Tooltip("Valor en monedas de oro")]
        [Min(1)]
        public int value = 5;

        [Tooltip("Peso en kg (afecta la física del cable y límite de inventario)")]
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
            // Auto asignar el color de aura según rareza
            auraColor = GetAuraColorForRarity(rarity);

            // Auto ajusta el valor sugerido según rareza
            if (value == 0)
            {
                value = GetSuggestedValueForRarity(rarity);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene el color de aura según la rareza
        /// </summary>
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

        /// <summary>
        /// Obtiene las dimensiones del grid que ocupa este item
        /// Retorna (width, height)
        /// </summary>
        public Vector2Int GetGridSize()
        {
            switch (shape)
            {
                case ItemShape.Single_1x1:
                    return new Vector2Int(1, 1);
                case ItemShape.Horizontal_2x1:
                    return new Vector2Int(2, 1);
                case ItemShape.Vertical_1x2:
                    return new Vector2Int(1, 2);
                case ItemShape.Horizontal_3x1:
                    return new Vector2Int(3, 1);
                case ItemShape.Vertical_1x3:
                    return new Vector2Int(1, 3);
                case ItemShape.Square_2x2:
                    return new Vector2Int(2, 2);
                case ItemShape.Square_3x3:
                    return new Vector2Int(3, 3);
                case ItemShape.LShape_2x2:
                    return new Vector2Int(2, 2);
                case ItemShape.TShape_3x2:
                    return new Vector2Int(3, 2);
                default:
                    return new Vector2Int(1, 1);
            }
        }

        /// <summary>
        /// Obtiene las posiciones locales que ocupa el item relativas a su origen (0,0)
        /// Por ejemplo, una forma L retorna las 3 celdas que ocupa
        /// </summary>
        public Vector2Int[] GetOccupiedCells()
        {
            switch (shape)
            {
                case ItemShape.Single_1x1:
                    return new Vector2Int[] { new Vector2Int(0, 0) };

                case ItemShape.Horizontal_2x1:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0)
                    };

                case ItemShape.Vertical_1x2:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1)
                    };

                case ItemShape.Horizontal_3x1:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0)
                    };

                case ItemShape.Vertical_1x3:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 2)
                    };

                case ItemShape.Square_2x2:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    };

                case ItemShape.Square_3x3:
                    return new Vector2Int[] {
                        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
                        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
                    };

                case ItemShape.LShape_2x2:
                    // Forma L:
                    // X .
                    // X X
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(0, 1),
                        new Vector2Int(1, 1)
                    };

                case ItemShape.TShape_3x2:
                    // Forma T:
                    // X X X
                    // . X .
                    return new Vector2Int[] {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(1, 1)
                    };

                default:
                    return new Vector2Int[] { new Vector2Int(0, 0) };
            }
        }

        #endregion

        /// <summary>
        /// Rarezas disponibles para los items
        /// </summary>
        public enum ItemRarity
        {
            Common,      // Gris - Valor: ~5
            Rare,        // Azul - Valor: ~10
            Epic,        // Morado - Valor: ~20
            Legendary    // Dorado - Valor: ~50
        }
    }
}