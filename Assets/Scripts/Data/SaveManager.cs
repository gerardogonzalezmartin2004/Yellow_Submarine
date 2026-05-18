using System;
using System.IO;
using UnityEngine;
using AbyssalReach.Data;

namespace AbyssalReach.Core
{
    // este script se encarga de gestionar las partidas guardadas, con soporte para múltiples ranuras (slots).
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance => instance;

        public const int MAX_SLOTS = 3;

        private const string FILE_PREFIX = "save_slot_";
        private const string FILE_EXT = ".json";

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private string Path(int slot) =>
            System.IO.Path.Combine(Application.persistentDataPath, $"{FILE_PREFIX}{slot}{FILE_EXT}");

        public void Save(int slot, SaveData data)
        {
            if (!SlotValid(slot)) return;

            data.slotIndex = slot;
            data.saveDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(Path(slot), json);

            Debug.Log($"[SaveManager] Slot {slot} guardado → {Path(slot)}");
        }

        public SaveData Load(int slot)
        {
            if (!SlotValid(slot) || !File.Exists(Path(slot))) return null;

            string json = File.ReadAllText(Path(slot));
            return JsonUtility.FromJson<SaveData>(json);
        }

        public SaveData[] LoadAllSlots()
        {
            var slots = new SaveData[MAX_SLOTS];
            for (int i = 0; i < MAX_SLOTS; i++)
                slots[i] = Load(i);
            return slots;
        }

        public bool SlotExists(int slot) => SlotValid(slot) && File.Exists(Path(slot));

        public void Delete(int slot)
        {
            if (!SlotValid(slot)) return;
            if (File.Exists(Path(slot)))
            {
                File.Delete(Path(slot));
                Debug.Log($"[SaveManager] Slot {slot} eliminado.");
            }
        }

        private bool SlotValid(int slot) => slot >= 0 && slot < MAX_SLOTS;
    }
}