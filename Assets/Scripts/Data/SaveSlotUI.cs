using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AbyssalReach.Data;

namespace AbyssalReach.UI
{
    // este script se encarga de gestionar la interfaz de cada ranura de guardado (slot), mostrando la información relevante si hay datos o un mensaje de vacío si no los hay. También maneja los eventos de clic para jugar o borrar la partida.
    public class SaveSlotUI : MonoBehaviour
    {
        [Header("Grupos (activa uno u otro según si hay datos)")]
        [SerializeField] private GameObject groupEmpty;  
        [SerializeField] private GameObject groupFilled; 

        [Header("Textos (en groupFilled)")]
        [SerializeField] private TextMeshProUGUI txtSlotName;
        [SerializeField] private TextMeshProUGUI txtDate;
        [SerializeField] private TextMeshProUGUI txtPlayTime;
        [SerializeField] private TextMeshProUGUI txtGold;

        [Header("Botones")]
        [SerializeField] private Button btnAction; 
        [SerializeField] private Button btnDelete; 

        public event Action OnActionClicked; 
        public event Action OnDeleteClicked;

        private void Awake()
        {
            btnAction.onClick.AddListener(() => OnActionClicked?.Invoke());
            if (btnDelete != null)
                btnDelete.onClick.AddListener(() => OnDeleteClicked?.Invoke());
        }

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
                if (txtSlotName != null) txtSlotName.text = $"Slot {slotIndex + 1}";
                return;
            }

            if (txtSlotName != null) txtSlotName.text = $"Partida {slotIndex + 1}";
            if (txtDate != null) txtDate.text = data.saveDate;
            if (txtPlayTime != null) txtPlayTime.text = FormatTime(data.totalPlayTime);
            if (txtGold != null) txtGold.text = $"{data.gold:F0} ☽";
        }

        private string FormatTime(float seconds)
        {
            int h = (int)(seconds / 3600);
            int m = (int)((seconds % 3600) / 60);
            int s = (int)(seconds % 60);
            return h > 0 ? $"{h}h {m}m" : $"{m}m {s}s";
        }
    }
}