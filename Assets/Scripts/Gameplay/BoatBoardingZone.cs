using AbyssalReach.Core;
using UnityEngine;
using System.Collections.Generic;

namespace AbyssalReach.Gameplay
{
    // Zona de abordaje del barco - VERSIÓN CORREGIDA
    // FIX CRÍTICO: Verifica múltiples condiciones antes de permitir subir
    [RequireComponent(typeof(BoxCollider))]
    public class BoatBoardingZone : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Tag que debe tener el buceador para ser detectado")]
        [SerializeField] private string diverTag = "Diver";

        [Header("UI Message")]
        [Tooltip("Mensaje que aparece en pantalla cuando puedes subir")]
        [SerializeField] private string boardingMessage = "Pulsa 'Espacio' para subir al barco";

        [Header("Loot Collection")]
        [Tooltip("Destruir los objetos físicos después de recogerlos")]
        [SerializeField] private bool destroyCollectedObjects = true;

        [Tooltip("Tiempo de espera antes de destruir objetos")]
        [SerializeField] private float destroyDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.3f);

        private bool diverInRange = false;
        private GameObject detectedDiver;
        private DiverGrapple diverGrapple;

        // Referencia a los controles
        private AbyssalReachControls controls;

        #region Ciclo de vida de Unity

        private void Awake()
        {
            controls = new AbyssalReachControls();

            // Asegurar que es trigger
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            Debug.Log("[" + gameObject.name + "] " + GetType().Name + " ENABLE - controls.DiverControls.enabled: " + controls.DiverControls.enabled);
            controls.Enable();
            controls.DiverControls.Enable();

            // Suscribir evento
            controls.DiverControls.Ascend.performed += OnBoardPressed;
        }

        private void OnDisable()
        {
            controls.DiverControls.Ascend.performed -= OnBoardPressed;
            controls.DiverControls.Disable();
            controls.Disable();
        }

        
        private void Update()
        {
            // Si el buzo está "en rango" pero ya NO estamos en modo Diving, limpiar
            if (diverInRange)
            {
                if (!GameController.Instance.IsDiving())
                {
                    // Limpiar estado inmediatamente
                    diverInRange = false;
                    detectedDiver = null;
                    diverGrapple = null;

                    if (showDebug)
                    {
                        Debug.Log("[BoardingZone] Estado limpiado - Ya no estamos en modo Diving");
                    }
                }
            }
        }

        #endregion

        #region Input Logic

        private void OnBoardPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            Debug.Log("=== BOARDING INTENT DETECTED ===");
            Debug.Log("GameController exists: " + (GameController.Instance != null));

            // 1. ¿Existe el GameController?
            if (GameController.Instance == null)
            {
                Debug.Log("IsDiving: " + GameController.Instance.IsDiving());
                Debug.Log("GetCurrentState: " + GameController.Instance.GetCurrentState());
                Debug.Log("diverInRange: " + diverInRange);
                Debug.Log("detectedDiver: " + (detectedDiver != null ? detectedDiver.name : "NULL"));

                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Bloqueado: GameController no existe");
                }
                return;
            }

            // 2. ¿Estamos realmente en modo Diving?
            if (!GameController.Instance.IsDiving())
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Bloqueado: No estamos en modo Diving");
                }
                return;
            }

            // 3. ¿El estado del GameController es Diving?
            if (GameController.Instance.GetCurrentState() != GameController.GameState.Diving)
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Bloqueado: Estado actual es " + GameController.Instance.GetCurrentState());
                }
                return;
            }

            // 4. ¿El buzo está físicamente en la zona?
            if (!diverInRange)
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Bloqueado: Buzo NO está en la zona");
                }
                return;
            }

            // 5. ¿Tenemos referencia al buzo?
            if (detectedDiver == null)
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Bloqueado: No hay buzo detectado");
                }
                return;
            }

           
            if (showDebug)
            {
                Debug.Log("[BoardingZone] ¡Todas las verificaciones pasadas! Subiendo al barco");
            }

            BoardTheBoat();
        }

        #endregion

        #region Trigger Detection

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(diverTag))
            {
                
                if ( GameController.Instance.IsDiving())
                {
                    diverInRange = true;
                    detectedDiver = other.gameObject;

                    diverGrapple = other.GetComponent<DiverGrapple>();

                    if (diverGrapple == null)
                    {
                        Debug.LogWarning("[BoardingZone] El buzo no tiene DiverGrapple");
                    }

                    if (showDebug)
                    {
                        Debug.Log("[BoardingZone] Diver entró en zona de abordaje");
                    }
                }
                else
                {
                    if (showDebug)
                    {
                        Debug.Log("[BoardingZone] Diver entró pero NO estamos en modo Diving - ignorando");
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(diverTag))
            {
                diverInRange = false;
                detectedDiver = null;
                diverGrapple = null;

                if (showDebug)
                {
                    Debug.Log("[BoardingZone] Diver salió de zona de abordaje");
                }
            }
        }

        #endregion

        #region Game Logic

        private void BoardTheBoat()
        {
            if (showDebug)
            {
                Debug.Log("[BoardingZone] ¡Subiendo al barco!");
            }

            CollectLootFromDiver();

            if (GameController.Instance != null)
            {
                GameController.Instance.EndDive();
            }
            else
            {
                Debug.LogError("[BoardingZone] GameController.Instance no encontrado");
            }
        }

        private void CollectLootFromDiver()
        {
            if (diverGrapple == null)
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] No hay DiverGrapple - saltando recolección");
                }
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[BoardingZone] InventoryManager.Instance es null");
                return;
            }

            List<LootObject> carriedObjects = diverGrapple.CollectCarriedObjects();

            if (carriedObjects.Count == 0)
            {
                if (showDebug)
                {
                    Debug.Log("[BoardingZone] No hay objetos para recoger");
                }
                return;
            }

            int totalValue = 0;
            int itemsCollected = 0;

            for (int i = 0; i < carriedObjects.Count; i = i + 1)
            {
                LootObject loot = carriedObjects[i];

                if (loot == null)
                {
                    continue;
                }

                Data.LootItemData originalData = loot.GetItemData();
                int currentValue = loot.GetCurrentValue();

                if (originalData == null)
                {
                    Debug.LogWarning("[BoardingZone] Objeto sin ItemData - ignorando");
                    continue;
                }

                bool added = InventoryManager.Instance.AddItem(originalData);

                if (added)
                {
                    totalValue = totalValue + currentValue;
                    itemsCollected = itemsCollected + 1;

                    if (showDebug)
                    {
                        string valueInfo = "";
                        int baseValue = loot.GetBaseValue();

                        if (currentValue < baseValue)
                        {
                            valueInfo = " (dañado: " + currentValue + "/" + baseValue + ")";
                        }

                        Debug.Log("[BoardingZone] Recogido: " + originalData.itemName + " - " + currentValue + "G" + valueInfo);
                    }

                    if (destroyCollectedObjects)
                    {
                        Destroy(loot.gameObject, destroyDelay);
                    }
                }
                else
                {
                    if (showDebug)
                    {
                        Debug.LogWarning("[BoardingZone] No se pudo añadir " + originalData.itemName + " al inventario");
                    }
                }
            }

            if (showDebug)
            {
                Debug.Log("[BoardingZone] Recolección completada");
                Debug.Log("[BoardingZone] Items: " + itemsCollected + " | Valor: " + totalValue + "G");
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
        }

        private void OnGUI()
        {
            if (!showDebug)
            {
                return;
            }

            // FIX: Solo mostrar mensaje si TODAS las condiciones se cumplen
            bool shouldShowMessage = diverInRange &&
                                    GameController.Instance != null &&
                                    GameController.Instance.IsDiving() &&
                                    GameController.Instance.GetCurrentState() == GameController.GameState.Diving;

            if (!shouldShowMessage)
            {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            float width = 500;
            float height = 40;
            Rect rect = new Rect((Screen.width - width) / 2, Screen.height - 150, width, height);

            // Fondo
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, "");
            GUI.color = Color.white;

            // Texto
            GUI.Label(rect, boardingMessage, style);

            // Info de objetos
            if (diverGrapple != null && diverGrapple.GetCarriedCount() > 0)
            {
                style.fontSize = 14;
                style.normal.textColor = Color.cyan;

                Rect infoRect = new Rect((Screen.width - width) / 2, Screen.height - 110, width, 30);

                string info = "Objetos a bordo: " + diverGrapple.GetCarriedCount();
                GUI.Label(infoRect, info, style);
            }

            // Debug estado (esquina)
            if (showDebug)
            {
                style.fontSize = 10;
                style.normal.textColor = Color.green;
                style.alignment = TextAnchor.UpperLeft;

                GUI.Label(new Rect(10, 200, 300, 20), "[BoardingZone] In Range: " + diverInRange, style);

                bool diving = GameController.Instance != null && GameController.Instance.IsDiving();
                GUI.Label(new Rect(10, 220, 300, 20), "[BoardingZone] Is Diving: " + diving, style);

                if (GameController.Instance != null)
                {
                    GUI.Label(new Rect(10, 240, 300, 20), "[BoardingZone] State: " + GameController.Instance.GetCurrentState(), style);
                }
            }
        }

        #endregion
    }
}