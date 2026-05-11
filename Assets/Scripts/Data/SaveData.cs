using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssalReach.Data
{
    // ─────────────────────────────────────────────────────────────────────────
    // Vector3 serializable (JsonUtility no serializa Vector3 directamente)
    // ─────────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────────
    // Item de inventario genérico
    // TODO: amplía los campos para que coincidan con tu ItemData real
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class InventoryItemData
    {
        public string itemId;     // ID único del item (string o enum.ToString())
        public int quantity;
        public float condition;   // 0-1 si tienes durabilidad, si no deja en 1
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Datos de una partida guardada
    // ─────────────────────────────────────────────────────────────────────────
    [Serializable]
    public class SaveData
    {
        // ── Meta ──────────────────────────────────────────────────────────────
        public int slotIndex;
        public string saveDate;        // "dd/MM/yyyy HH:mm"
        public float totalPlayTime;   // segundos acumulados

        // ── Posiciones ────────────────────────────────────────────────────────
        public SerializableVector3 diverPosition;
        public SerializableVector3 boatPosition;

        // ── Economía ──────────────────────────────────────────────────────────
        public float gold;

        // ── Tienda / Mejoras ──────────────────────────────────────────────────
        // Lista de IDs de upgrades comprados (ej: "oxygen_tank_2", "lantern_boost")
        public List<string> purchasedUpgrades = new List<string>();

        // ── Props del entorno ─────────────────────────────────────────────────
        // IDs de props ya recogidos (para que no reaparezcan al cargar)
        public List<string> collectedPropIds = new List<string>();

        // ── Inventarios ───────────────────────────────────────────────────────
        public List<InventoryItemData> boatInventory = new List<InventoryItemData>();
        public List<InventoryItemData> diverInventory = new List<InventoryItemData>();

        // ── Progresión ────────────────────────────────────────────────────────
        public float score;
        public int currentLevel;   // si tienes sistema de niveles/zonas
    }
}