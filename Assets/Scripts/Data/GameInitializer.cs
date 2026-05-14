using UnityEngine;
using System.Collections.Generic;
using AbyssalReach.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AbyssalReach.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("References (arrastra desde la escena)")]
        [SerializeField] private Transform diverTransform;
        [SerializeField] private Transform boatTransform;

        [Header("Inventory References")]
        [SerializeField] private ItemGrid boatItemGrid;
        [SerializeField] private GameObject inventoryItemPrefab;

        [Header("Item Database")]
        [Tooltip("Se rellena automaticamente en Editor. Haz click en el componente para actualizarlo.")]
        [SerializeField] private ItemData[] itemDatabase;

#if UNITY_EDITOR
        private void OnValidate()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData");
            itemDatabase = new ItemData[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                itemDatabase[i] = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            }
            EditorUtility.SetDirty(this);
        }
#endif

        private float sessionTime;
        private float loadedPlayTime;

        public static GameInitializer Instance { get; private set; }

        public int ActiveSlot { get; private set; } = -1;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (SceneLoader.Instance == null) return;

            ActiveSlot = SceneLoader.Instance.PendingSlot;

            if (SceneLoader.Instance.IsNewGame)
            {
                Debug.Log("[GameInitializer] Nueva partida, slot " + ActiveSlot);
                InitNewGame();
            }
            else
            {
                Debug.Log("[GameInitializer] Cargando partida, slot " + ActiveSlot);
                LoadGame(ActiveSlot);
            }
        }

        private void Update()
        {
            if (Time.timeScale > 0f)
                sessionTime += Time.unscaledDeltaTime;
        }

        public float GetTotalPlayTime() => loadedPlayTime + sessionTime;

        private void InitNewGame()
        {
            sessionTime = 0f;
            loadedPlayTime = 0f;
        }

        private void LoadGame(int slot)
        {
            SaveData data = SaveManager.Instance.Load(slot);
            if (data == null)
            {
                Debug.LogWarning("[GameInitializer] No existe save en slot " + slot + ". Iniciando nueva partida.");
                InitNewGame();
                return;
            }

            loadedPlayTime = data.totalPlayTime;
            sessionTime = 0f;

            if (diverTransform != null && data.diverPosition != null)
                diverTransform.position = data.diverPosition.ToVector3();

            if (boatTransform != null && data.boatPosition != null)
                boatTransform.position = data.boatPosition.ToVector3();

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.SetGold((int)data.gold);

            LoadDiverInventory(data.diverInventory);
            LoadBoatGrid(data.boatInventory);

            Debug.Log("[GameInitializer] Partida cargada: slot " + slot + ", oro " + data.gold + ", tiempo " + data.totalPlayTime + "s");
        }

        private void LoadDiverInventory(List<InventoryItemData> savedItems)
        {
            if (InventoryManager.Instance == null) return;
            if (savedItems == null || savedItems.Count == 0) return;

            InventoryManager.Instance.DiscardDiverItems();
            DiverInventory inv = InventoryManager.Instance.GetDiverInventory();

            foreach (InventoryItemData saved in savedItems)
            {
                if (string.IsNullOrEmpty(saved.itemId)) continue;
                ItemData found = FindItemById(saved.itemId);
                if (found == null)
                {
                    Debug.LogWarning("[GameInitializer] Item del buzo no encontrado en base de datos: " + saved.itemId);
                    continue;
                }
                string error;
                inv.TryAddItem(found, null, out error);
            }
        }

        private void LoadBoatGrid(List<InventoryItemData> savedItems)
        {
            if (boatItemGrid == null)
            {
                Debug.LogWarning("[GameInitializer] boatItemGrid no asignado. No se pueden cargar items del barco.");
                return;
            }
            if (inventoryItemPrefab == null)
            {
                Debug.LogWarning("[GameInitializer] inventoryItemPrefab no asignado. No se pueden cargar items del barco.");
                return;
            }
            if (savedItems == null || savedItems.Count == 0) return;

            boatItemGrid.ClearAllItems();

            foreach (InventoryItemData saved in savedItems)
            {
                if (string.IsNullOrEmpty(saved.itemId)) continue;
                ItemData found = FindItemById(saved.itemId);
                if (found == null)
                {
                    Debug.LogWarning("[GameInitializer] Item del barco no encontrado en base de datos: " + saved.itemId);
                    continue;
                }

                GameObject go = Instantiate(inventoryItemPrefab);
                InventoryItem invItem = go.GetComponent<InventoryItem>();
                if (invItem == null)
                {
                    Destroy(go);
                    continue;
                }

                invItem.Set(found);

                go.transform.SetParent(boatItemGrid.transform, false);

                Vector2Int? pos = boatItemGrid.FindSpaceForObject(invItem);
                if (pos.HasValue)
                {
                    boatItemGrid.PlaceItem(invItem, pos.Value.x, pos.Value.y);
                    ItemPositionMemory memory = invItem.GetComponent<ItemPositionMemory>();
                    memory?.SaveCurrentPosition(boatItemGrid);
                }
                else
                {
                    Destroy(go);
                    Debug.LogWarning("[GameInitializer] Sin espacio en el grid del barco para: " + found.name);
                }
            }
        }

        public SaveData CollectSaveData()
        {
            var data = new SaveData
            {
                totalPlayTime = GetTotalPlayTime(),
            };

            if (diverTransform != null)
                data.diverPosition = SerializableVector3.From(diverTransform.position);
            if (boatTransform != null)
                data.boatPosition = SerializableVector3.From(boatTransform.position);

            if (CurrencyManager.Instance != null)
                data.gold = CurrencyManager.Instance.GetGold();

            if (InventoryManager.Instance != null)
            {
                List<ItemData> diverItems = InventoryManager.Instance.GetDiverInventory().GetItemDatas();
                foreach (ItemData item in diverItems)
                {
                    if (item == null) continue;
                    data.diverInventory.Add(new InventoryItemData
                    {
                        itemId = item.name,
                        quantity = 1,
                        condition = 1f
                    });
                }
            }

            if (boatItemGrid != null)
            {
                List<InventoryItem> boatItems = boatItemGrid.GetAllItems();
                foreach (InventoryItem item in boatItems)
                {
                    if (item == null || item.itemData == null) continue;
                    data.boatInventory.Add(new InventoryItemData
                    {
                        itemId = item.itemData.name,
                        quantity = 1,
                        condition = 1f
                    });
                }
            }

            return data;
        }

        private ItemData FindItemById(string id)
        {
            if (itemDatabase == null) return null;
            foreach (ItemData item in itemDatabase)
                if (item != null && item.name == id)
                    return item;
            return null;
        }
    }
}
