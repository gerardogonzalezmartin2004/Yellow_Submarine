using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Data;

namespace AbyssalReach.UI
{
    /// <summary>
    /// Componente que representa un slot de guardado en la UI.
    /// Sirve tanto para el Menú Principal (cargar) como para la Pausa (guardar).
    /// Asigna los elementos en el Inspector.
    /// </summary>
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Grupos (activa uno u otro según si hay datos)")]
        [SerializeField] private GameObject groupEmpty;   // Panel "Slot vacío"
        [SerializeField] private GameObject groupFilled;  // Panel con datos

        [Header("Textos (en groupFilled)")]
        [SerializeField] private TextMeshProUGUI txtSlotName;
        [SerializeField] private TextMeshProUGUI txtDate;
        [SerializeField] private TextMeshProUGUI txtPlayTime;
        [SerializeField] private TextMeshProUGUI txtGold;

        [Header("Botones")]
        [SerializeField] private Button btnAction;  // "Jugar" o "Guardar aquí"
        [SerializeField] private Button btnDelete;  // Sólo en Menú Principal

        // ── Eventos ───────────────────────────────────────────────────────────
        public event Action OnActionClicked;   // Jugar o Guardar
        public event Action OnDeleteClicked;

        private void Awake()
        {
            btnAction.onClick.AddListener(() => OnActionClicked?.Invoke());
            if (btnDelete != null)
                btnDelete.onClick.AddListener(() => OnDeleteClicked?.Invoke());
        }

        // ── Método principal: rellena la UI según los datos del slot ──────────
        public void Populate(int slotIndex, SaveData data, string actionLabel = "Jugar")
        {
            bool hasData = data != null;

            groupEmpty.SetActive(!hasData);
            groupFilled.SetActive(hasData);

            btnAction.GetComponentInChildren<TextMeshProUGUI>().text = actionLabel;

            if (btnDelete != null)
                btnDelete.gameObject.SetActive(hasData);

            if (!hasData)
            {
                // Slot vacío: el botón de acción sigue visible para nueva partida
                if (txtSlotName != null) txtSlotName.text = $"Slot {slotIndex + 1}";
                return;
            }

            if (txtSlotName != null) txtSlotName.text = $"Partida {slotIndex + 1}";
            if (txtDate != null) txtDate.text = data.saveDate;
            if (txtPlayTime != null) txtPlayTime.text = FormatTime(data.totalPlayTime);
            if (txtGold != null) txtGold.text = $"{data.gold:F0} ☽";
        }

        // ── Util ──────────────────────────────────────────────────────────────
        private string FormatTime(float seconds)
        {
            int h = (int)(seconds / 3600);
            int m = (int)((seconds % 3600) / 60);
            int s = (int)(seconds % 60);
            return h > 0 ? $"{h}h {m}m" : $"{m}m {s}s";
        }
    }
}