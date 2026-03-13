using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour
{
   public ItemData itemData;
    public int HEIGHT
    { 

        get
        {
            if(rotated == false)
            {
                return itemData.height;
            }
            else
            {
                return itemData.width;
            }
        }
    }
    public int WIDTH
    {
        get
        {
            if (rotated == false)
            {
                return itemData.width;
            }
            else
            {
                return itemData.height;
            }
        }
    }
    public int onGridPositionX;
    public int onGridPositionY;

    public bool rotated = false;

    internal void Rotate()
    {
        rotated = !rotated;
       RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.rotation = Quaternion.Euler(0, 0, rotated == true? 90f : 0f);
      
    }

    internal void Set(ItemData itemData)
    {
       this.itemData = itemData;
        GetComponent<Image>().sprite = itemData.itemIcon;

        Vector2 size = new Vector2();
        size.x = WIDTH * ItemGrid.tileSizeWidht;
        size.y = HEIGHT* ItemGrid.tileSizeHeight;
        GetComponent<RectTransform>().sizeDelta = size;
    }
}
