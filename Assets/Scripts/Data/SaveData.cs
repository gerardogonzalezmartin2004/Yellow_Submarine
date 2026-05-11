using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalReach.Data
{

    [Serializable]
    public class SerializableVector3
    {
        public float x, y, z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x; this.y = y; this.z = z;
        }

        public static SerializableVector3 From(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    
    [Serializable]
    public class InventoryItemData
    {
        public string itemId;   
        public int quantity;
        public float condition;   
    }

    
    [Serializable]
    public class SaveData
    {
        public int slotIndex;
        public string saveDate;        
        public float totalPlayTime;   

        public SerializableVector3 diverPosition;
        public SerializableVector3 boatPosition;

        public float gold;

       
        public List<string> purchasedUpgrades = new List<string>();

        
        public List<string> collectedPropIds = new List<string>();

        public List<InventoryItemData> boatInventory = new List<InventoryItemData>();
        public List<InventoryItemData> diverInventory = new List<InventoryItemData>();

        public float score;
        public int currentLevel;   
    }
}