using UnityEngine;
using System.Collections.Generic;
using System;


public class InventoryController : MonoBehaviour
{
    [HideInInspector]
    private ItemGrid selectedItemGrid;
    public ItemGrid SelectedItemGrid 
    {
        get => selectedItemGrid; 
        set
        {
            selectedItemGrid = value;
            inventoryHighlight.SetParent(value);
        }
    }

    InventoryItem selectedItem;
    InventoryItem overlapItem;
    RectTransform rectTransform;

    [SerializeField] List<ItemData> items;
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform canvasTrasnform;

    InventotyHighlight inventoryHighlight;

    private void Awake()
    {
        inventoryHighlight = GetComponent<InventotyHighlight>();
    }

    private void Update()
    {
        //  Hacer que el objeto cogido siga al ratón en cada frame
        ItemIconHover();
        ItemIconDrag();


        if (Input.GetKeyDown(KeyCode.Q))
        {
            if(selectedItem == null)
            {
              CreateRamdomItem();
            }
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            InsertRamdomItem();
        }


        if(Input.GetKeyDown(KeyCode.R))
        {
           RotateItem();
        }

        // Si el ratón no está sobre la cuadrícula, no hacemos nada con los clics
        if (selectedItemGrid == null)
        {
            inventoryHighlight.Show(false);
            return;
        }

        HandleHighlight();

        // Lógica de clics
        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseButoonPress();
        }
    }

    private void RotateItem()
    {
        if(selectedItem == null)
        {
            return;
        }
        selectedItem.Rotate();
         
    }

    private void InsertRamdomItem()
    {
        if (selectedItemGrid == null)
        {
            return;
        }
        CreateRamdomItem();
       InventoryItem itemToIsert = selectedItem;
        selectedItem = null;
        InserItem(itemToIsert);
    }

    private void InserItem(InventoryItem itemToIsert)
    {
        
        Vector2Int? posOnGrid = selectedItemGrid.FindSpaceForObeject(itemToIsert);
        if (posOnGrid == null)
        {
            return;
        }
       selectedItemGrid.PlaceItem(itemToIsert, posOnGrid.Value.x, posOnGrid.Value.y);
    }

    Vector2Int oldPosition;
    InventoryItem itemToHighlight;
    private void HandleHighlight()
    {
       Vector2Int positionOnGrid = GetTitleGridPosition();
        if(oldPosition == positionOnGrid)
        {
            return;
        }
            oldPosition = positionOnGrid;
        if (selectedItem == null)
        {
            itemToHighlight = selectedItemGrid.GetItem(positionOnGrid.x, positionOnGrid.y);
            if (itemToHighlight != null)
            {
                inventoryHighlight.Show(true);
                inventoryHighlight.SetSize(itemToHighlight);
                inventoryHighlight.SetPosition(selectedItemGrid, itemToHighlight);

            }
            else       
            {
                inventoryHighlight.Show(false);
            }
        }
       else
       {
            inventoryHighlight.Show(selectedItemGrid.BoundyCheck(positionOnGrid.x, positionOnGrid.y, selectedItem.WIDTH, selectedItem.HEIGHT));
            inventoryHighlight.SetSize(selectedItem);
            inventoryHighlight.SetPosition(selectedItemGrid, selectedItem, positionOnGrid.x, positionOnGrid.y);
       }
      
    }

    private void CreateRamdomItem()
    {
       InventoryItem inventoryItem = Instantiate(itemPrefab).GetComponent<InventoryItem>();
        selectedItem = inventoryItem;

        rectTransform = inventoryItem.GetComponent<RectTransform>();
        rectTransform.SetParent(canvasTrasnform);
        rectTransform.SetAsLastSibling();

        int selectedItemID = UnityEngine.Random.Range(0, items.Count);
        inventoryItem.Set(items[selectedItemID]);
    }

    private void LeftMouseButoonPress()
    {
        Vector2Int tileGridPosition = GetTitleGridPosition();

        if (selectedItem == null)
        {
            // Intentamos recoger lo que haya en esa casilla
            PickUpItem(tileGridPosition);
        }

        else
        {
            PlaceItem(tileGridPosition);

        }
    }

    private Vector2Int GetTitleGridPosition()
    {
        Vector2 position = Input.mousePosition;
        if (selectedItem != null)
        {
            position.x -= (selectedItem.WIDTH - 1) * ItemGrid.tileSizeWidht / 2;
            position.y += (selectedItem.HEIGHT - 1) * ItemGrid.tileSizeHeight / 2;
        }
        Vector2Int tileGridPosition = selectedItemGrid.GetTitleGridPosiiton(position);
        return tileGridPosition;
    }

    private void PlaceItem(Vector2Int tileGridPosition)
    {
        // Lo soltamos en la casilla
        bool complete = selectedItemGrid.PlaceItem(selectedItem, tileGridPosition.x, tileGridPosition.y, ref overlapItem);
        if(complete)
        {
            selectedItem = null;
            if(overlapItem != null)
            {
                // Si había un objeto, lo recogemos
                selectedItem = overlapItem;
                overlapItem = null;
                rectTransform = selectedItem.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
            }
        }
        
    }

    private void PickUpItem(Vector2Int tileGridPosition)
    {
        selectedItem = selectedItemGrid.PickUpItem(tileGridPosition.x, tileGridPosition.y);
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();

        }
    }

    private void ItemIconDrag()
    {
        if (selectedItem != null)
        {
            rectTransform.position = Input.mousePosition;
        }
    }

    private void ItemIconHover()
    {
        if (selectedItem != null)
        {
            rectTransform = selectedItem.GetComponent<RectTransform>();
            rectTransform.position = Input.mousePosition;
        }
    }
}