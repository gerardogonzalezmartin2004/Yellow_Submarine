using System;
using UnityEngine;

public class ItemGrid : MonoBehaviour
{
    public const float tileSizeWidht = 32f;
    public const float tileSizeHeight = 32f;

    InventoryItem[,] inventoryItemSlots;
    RectTransform rectTransform;

    [SerializeField] int gridWidth = 20;
    [SerializeField] int gridHeight = 10;


    [SerializeField] GameObject inventoryItemPrefab;
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        Init(gridWidth, gridHeight);    
    }

    private void Init(int width, int height)
    {
        inventoryItemSlots = new InventoryItem[width, height];
        Vector2 size = new Vector2(width * tileSizeWidht, height * tileSizeHeight);
        rectTransform.sizeDelta = size;
    }
    Vector2 positionOnTheGrid = new Vector2();
    Vector2Int titleGridPosition = new Vector2Int();
    public Vector2Int GetTitleGridPosiiton(Vector2 mousePOsition)
    {
        positionOnTheGrid.x = mousePOsition.x - rectTransform.position.x;
        positionOnTheGrid.y =  rectTransform.position.y - mousePOsition.y;

        titleGridPosition.x = (int)(positionOnTheGrid.x / tileSizeWidht);
        titleGridPosition.y = (int)(positionOnTheGrid.y / tileSizeHeight);

        return titleGridPosition;

    }
    public bool PlaceItem(InventoryItem inventoryItem, int posX, int posY, ref InventoryItem overlapItem)
    {
        if (BoundyCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT) == false)
        {
            return false;
        }

        if (OverlapCheck(posX, posY, inventoryItem.WIDTH, inventoryItem.HEIGHT, ref overlapItem) == false)
        {
            overlapItem = null;
            return false;
        }
        if (overlapItem != null)
        {
            CleanGridReference(overlapItem);
        }

        PlaceItem(inventoryItem, posX, posY);

        return true;
    }

    public  void PlaceItem(InventoryItem inventoryItem, int posX, int posY)
    {
        RectTransform rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(this.rectTransform);

        for (int i = 0; i < inventoryItem.WIDTH; i++)
        {
            for (int j = 0; j < inventoryItem.HEIGHT; j++)
            {
                inventoryItemSlots[posX + i, posY + j] = inventoryItem;
            }
        }
        inventoryItem.onGridPositionX = posX;
        inventoryItem.onGridPositionY = posY;
        Vector2 position = CalculatePositionOnGrid(inventoryItem, posX, posY);

        rectTransform.localPosition = position;
    }

    public Vector2 CalculatePositionOnGrid(InventoryItem inventoryItem, int posX, int posY)
    {
        Vector2 position = new Vector2();
        position.x = posX * tileSizeWidht + tileSizeWidht * inventoryItem.WIDTH / 2;
        position.y = -(posY * tileSizeHeight + tileSizeHeight * inventoryItem.HEIGHT / 2);
        return position;
    }

    private bool OverlapCheck(int posX, int posY, int width, int height, ref InventoryItem overlapItem)
    {
        for(int i = 0; i < width; i++)
        {
            for(int j = 0; j < height; j++)
            {
                if(inventoryItemSlots[posX + i, posY + j] != null)
                {
                    if (overlapItem == null)
                    {
                        overlapItem = inventoryItemSlots[posX + i, posY + j];
                    }
                    else
                    {
                        if(overlapItem != inventoryItemSlots[posX + i, posY + j])
                        {

                            return false;
                        }
                    }
                   
                }
            }
        }

        return true;
    }
    private bool CheckAvailableSpace(int posX, int posY, int width, int height)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (inventoryItemSlots[posX + i, posY + j] != null)
                {
                    return false;

                }
            }
        }

        return true;
    }
    public InventoryItem PickUpItem(int x, int y)
    {
        InventoryItem toReturn = inventoryItemSlots[x, y];
        if (toReturn == null)
        {
            return null;
        }

        CleanGridReference(toReturn);
        inventoryItemSlots[x, y] = null;
        return toReturn;
    }

    private void CleanGridReference(InventoryItem item)
    {
        for (int i = 0; i < item.WIDTH; i++)
        {
            for (int j = 0; j < item.HEIGHT; j++)
            {
                inventoryItemSlots[item.onGridPositionX + i, item.onGridPositionY + j] = null;
            }
        }
    }

    bool PositionCheck(int posX, int posY)
    {
        if(posX< 0 || posY < 0)
        {
            return false;
        }
        if(posX >= gridWidth || posY >= gridHeight)
        {
            return false;
        }

        return true;
    }
    public bool BoundyCheck(int posX, int posY, int width, int height)
    {
        if(PositionCheck(posX, posY) == false)
        {
            return false;
        }
        posX += width-1;
        posY += height-1;
        if (PositionCheck(posX, posY) == false)
        {
            return false;
        }
        return true;
    }

    internal InventoryItem GetItem(int x, int y)
    {
        return inventoryItemSlots[x, y];
    }

    public Vector2Int? FindSpaceForObeject(InventoryItem itemToIsert) //interrogacion q pueda ser null el valor de retorno, si no encuentra espacio retorna null, si encuentra espacio retorna la posicion en el grid para colocar el item
    {
        int height = gridHeight - itemToIsert.HEIGHT + 1 ;
        int width = gridWidth - itemToIsert.WIDTH + 1;

        for (int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                if(CheckAvailableSpace(x, y, itemToIsert.WIDTH, itemToIsert.HEIGHT) == true)
                {
                    return new Vector2Int(x, y);
                }
                
            }
        }
        return null;
    }
}
