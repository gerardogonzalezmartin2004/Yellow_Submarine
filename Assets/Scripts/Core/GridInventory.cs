using System;
using System.Collections.Generic;
using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    /// <summary>
    /// Clase core que maneja un inventario basado en grid 2D con sistema de peso.
    /// NO es un MonoBehaviour - es una clase pura de C# para ser instanciada por InventoryManager.
    /// Soporta formas de items complejas y serialización para Save/Load.
    /// </summary>
    [System.Serializable]
    public class GridInventory
    {
        // === CONFIGURACIÓN DEL GRID ===
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;

        // === SISTEMA DE PESO ===
        [SerializeField] private float currentWeight;
        [SerializeField] private float maxWeightCapacity;

        // === ALMACENAMIENTO ===
        // Cada celda del grid puede estar vacía (null) o contener una referencia a un GridItem
        [SerializeField] private GridItem[,] gridCells;

        // Lista de todos los items únicos en este inventario
        [SerializeField] private List<GridItem> items;

        #region Constructor & Initialization

        /// <summary>
        /// Constructor para crear un nuevo inventario grid
        /// </summary>
        public GridInventory(int width, int height, float maxWeight)
        {
            gridWidth = width;
            gridHeight = height;
            maxWeightCapacity = maxWeight;
            currentWeight = 0f;

            gridCells = new GridItem[width, height];
            items = new List<GridItem>();
        }

        #endregion

        #region Adding Items

        /// <summary>
        /// Intenta añadir un item al inventario.
        /// Retorna true si tuvo éxito, false si no hay espacio o excede el peso.
        /// </summary>
        public bool TryAddItem(LootItemData itemData, out string errorMessage)
        {
            errorMessage = "";

            // Validación
            if (itemData == null)
            {
                errorMessage = "Item inválido";
                return false;
            }

            // Verificar peso
            if (currentWeight + itemData.weight > maxWeightCapacity)
            {
                errorMessage = "Demasiado pesado (" + itemData.weight + "kg)";
                return false;
            }

            // Buscar posición disponible
            Vector2Int position = FindAvailablePosition(itemData);

            if (position.x == -1)
            {
                errorMessage = "No hay espacio en el grid";
                return false;
            }

            // Crear el GridItem
            GridItem newItem = new GridItem(itemData, position);

            // Colocarlo en el grid
            PlaceItemInGrid(newItem);

            // Añadir a la lista
            items.Add(newItem);

            // Actualizar peso
            currentWeight += itemData.weight;

            return true;
        }

        /// <summary>
        /// Busca la primera posición disponible donde quepa el item
        /// Retorna (-1, -1) si no encuentra espacio
        /// </summary>
        private Vector2Int FindAvailablePosition(LootItemData itemData)
        {
            Vector2Int[] occupiedCells = itemData.GetOccupiedCells();

            // Recorrer todo el grid de izquierda a derecha, arriba a abajo
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    // Verificar si el item cabe en esta posición
                    if (CanPlaceItemAt(x, y, occupiedCells))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            return new Vector2Int(-1, -1); // No hay espacio
        }

        /// <summary>
        /// Verifica si un item puede ser colocado en una posición específica
        /// </summary>
        private bool CanPlaceItemAt(int startX, int startY, Vector2Int[] occupiedCells)
        {
            foreach (Vector2Int offset in occupiedCells)
            {
                int checkX = startX + offset.x;
                int checkY = startY + offset.y;

                // Verificar límites del grid
                if (checkX < 0 || checkX >= gridWidth || checkY < 0 || checkY >= gridHeight)
                {
                    return false;
                }

                // Verificar si la celda está ocupada
                if (gridCells[checkX, checkY] != null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Coloca un item en el grid marcando todas sus celdas
        /// </summary>
        private void PlaceItemInGrid(GridItem item)
        {
            Vector2Int[] occupiedCells = item.itemData.GetOccupiedCells();

            foreach (Vector2Int offset in occupiedCells)
            {
                int x = item.gridPosition.x + offset.x;
                int y = item.gridPosition.y + offset.y;

                gridCells[x, y] = item;
            }
        }

        #endregion

        #region Removing Items

        /// <summary>
        /// Elimina un item del inventario
        /// </summary>
        public bool RemoveItem(GridItem item)
        {
            if (item == null || !items.Contains(item))
            {
                return false;
            }

            // Limpiar las celdas del grid
            ClearItemFromGrid(item);

            // Quitar de la lista
            items.Remove(item);

            // Actualizar peso
            currentWeight -= item.itemData.weight;

            return true;
        }

        /// <summary>
        /// Elimina un item por su posición en el grid
        /// </summary>
        public bool RemoveItemAt(int x, int y)
        {
            GridItem item = GetItemAtSlot(x, y);

            if (item != null)
            {
                return RemoveItem(item);
            }

            return false;
        }

        /// <summary>
        /// Limpia todas las celdas ocupadas por un item
        /// </summary>
        private void ClearItemFromGrid(GridItem item)
        {
            Vector2Int[] occupiedCells = item.itemData.GetOccupiedCells();

            foreach (Vector2Int offset in occupiedCells)
            {
                int x = item.gridPosition.x + offset.x;
                int y = item.gridPosition.y + offset.y;

                if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
                {
                    gridCells[x, y] = null;
                }
            }
        }

        /// <summary>
        /// Elimina TODOS los items del inventario
        /// </summary>
        public void Clear()
        {
            items.Clear();
            gridCells = new GridItem[gridWidth, gridHeight];
            currentWeight = 0f;
        }

        #endregion

        #region Querying & API

        /// <summary>
        /// Obtiene el item en una posición específica del grid
        /// Retorna null si la celda está vacía
        /// </summary>
        public GridItem GetItemAtSlot(int x, int y)
        {
            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            {
                return null;
            }

            return gridCells[x, y];
        }

        /// <summary>
        /// Obtiene una copia de la lista de items
        /// </summary>
        public List<GridItem> GetAllItems()
        {
            return new List<GridItem>(items);
        }

        /// <summary>
        /// Obtiene las dimensiones del grid
        /// </summary>
        public Vector2Int GetGridSize()
        {
            return new Vector2Int(gridWidth, gridHeight);
        }

        /// <summary>
        /// Obtiene el peso actual del inventario
        /// </summary>
        public float GetCurrentWeight()
        {
            return currentWeight;
        }

        /// <summary>
        /// Obtiene la capacidad máxima de peso
        /// </summary>
        public float GetMaxWeight()
        {
            return maxWeightCapacity;
        }

        /// <summary>
        /// Retorna el número de items únicos
        /// </summary>
        public int GetItemCount()
        {
            return items.Count;
        }

        /// <summary>
        /// Verifica si el inventario está vacío
        /// </summary>
        public bool IsEmpty()
        {
            return items.Count == 0;
        }

        /// <summary>
        /// Verifica si hay espacio para un item específico
        /// </summary>
        public bool CanFitItem(LootItemData itemData)
        {
            if (itemData == null) return false;

            // Verificar peso
            if (currentWeight + itemData.weight > maxWeightCapacity)
            {
                return false;
            }

            // Verificar espacio en grid
            Vector2Int position = FindAvailablePosition(itemData);
            return position.x != -1;
        }

        #endregion

        #region Upgrade System

        /// <summary>
        /// Aumenta el tamaño del grid (solo si los nuevos valores son mayores)
        /// IMPORTANTE: Los items existentes se mantienen en sus posiciones
        /// </summary>
        public void UpgradeGridSize(int newWidth, int newHeight)
        {
            if (newWidth <= gridWidth && newHeight <= gridHeight)
            {
                Debug.LogWarning("[GridInventory] El nuevo tamaño debe ser mayor al actual");
                return;
            }

            // Crear nuevo grid más grande
            GridItem[,] newGrid = new GridItem[newWidth, newHeight];

            // Copiar items existentes
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    newGrid[x, y] = gridCells[x, y];
                }
            }

            // Reemplazar
            gridCells = newGrid;
            gridWidth = newWidth;
            gridHeight = newHeight;

            Debug.Log("[GridInventory] Grid actualizado a " + newWidth + "x" + newHeight);
        }

        /// <summary>
        /// Aumenta la capacidad de peso
        /// </summary>
        public void UpgradeWeightCapacity(float additionalCapacity)
        {
            maxWeightCapacity += additionalCapacity;
            Debug.Log("[GridInventory] Capacidad de peso aumentada en +" + additionalCapacity + "kg (Total: " + maxWeightCapacity + "kg)");
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Calcula el valor total de todos los items
        /// </summary>
        public int CalculateTotalValue()
        {
            int total = 0;

            foreach (GridItem item in items)
            {
                total += item.itemData.value;
            }

            return total;
        }

        #endregion
    }

    /// <summary>
    /// Representa un item colocado en el grid
    /// Almacena tanto los datos del item como su posición
    /// </summary>
    [System.Serializable]
    public class GridItem
    {
        public LootItemData itemData;
        public Vector2Int gridPosition; // Posición de su esquina superior izquierda

        public GridItem(LootItemData data, Vector2Int position)
        {
            itemData = data;
            gridPosition = position;
        }
    }
}