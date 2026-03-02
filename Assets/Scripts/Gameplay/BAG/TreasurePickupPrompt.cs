using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace AbyssalReach.Gameplay
{
    // Sistema de prompt UI que aparece sobre tesoros cuando estás cerca
    // Muestra tecla E/X o botón de gamepad
    [RequireComponent(typeof(Canvas))]
    public class TreasurePickupPrompt : MonoBehaviour
    {
        [Header("Prompt Settings")]
        [Tooltip("Distancia máxima para mostrar el prompt")]
        [SerializeField] private float detectionRadius = 2.5f;

        [Tooltip("Altura sobre el tesoro")]
        [SerializeField] private float promptHeight = 0.5f;

        [Tooltip("Tecla para recoger (teclado)")]
        [SerializeField] private KeyCode pickupKey = KeyCode.E;

        [Tooltip("Nombre de la acción en Input System")]
        [SerializeField] private string inputActionName = "Interact";

        [Header("Visual")]
        [Tooltip("Prefab del prompt UI (o se crea automáticamente)")]
        [SerializeField] private GameObject promptPrefab;

        [Tooltip("Texto del prompt")]
        [SerializeField] private string promptText = "[E] Recoger";

        [Tooltip("Color del texto")]
        [SerializeField] private Color textColor = Color.white;

        [Tooltip("Color del fondo")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

        [Tooltip("Escala del prompt")]
        [SerializeField] private float promptScale = 0.5f;

        [Header("Animation")]
        [Tooltip("¿Animar el prompt?")]
        [SerializeField] private bool animatePrompt = true;

        [Tooltip("Velocidad de bounce")]
        [SerializeField] private float bounceSpeed = 2f;

        [Tooltip("Amplitud de bounce")]
        [SerializeField] private float bounceAmplitude = 0.1f;

        [Header("References")]
        [Tooltip("Transform del diver")]
        [SerializeField] private Transform diverTransform;

        [Tooltip("Bolsa del diver")]
        [SerializeField] private DiverBagSoftbody diverBag;

        [Tooltip("Layer de tesoros")]
        [SerializeField] private LayerMask treasureLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // UI Components
        private Canvas canvas;
        private GameObject activePrompt;
        private TextMeshProUGUI promptTextMesh;
        private Image promptBackground;
        private RectTransform promptRect;

        // Current state
        private LootObject nearestTreasure;
        private float bounceTimer;
        private Vector3 basePosition;

        #region Unity Lifecycle

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            ConfigureCanvas();

            if (diverTransform == null)
            {
                GameObject diver = GameObject.FindGameObjectWithTag("Player");
                if (diver != null)
                {
                    diverTransform = diver.transform;
                }
            }

            if (diverBag == null && diverTransform != null)
            {
                diverBag = diverTransform.GetComponentInChildren<DiverBagSoftbody>();
            }
        }

        private void Update()
        {
            FindNearestTreasure();
            UpdatePrompt();
            CheckInput();
        }

        #endregion

        #region Canvas Setup

        private void ConfigureCanvas()
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            // Configurar para que siempre mire a la cámara
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);
            canvasRect.localScale = Vector3.one * promptScale;
        }

        #endregion

        #region Prompt Creation

        private void CreatePrompt()
        {
            if (promptPrefab != null)
            {
                activePrompt = Instantiate(promptPrefab, transform);
                promptTextMesh = activePrompt.GetComponentInChildren<TextMeshProUGUI>();
                promptBackground = activePrompt.GetComponentInChildren<Image>();
                promptRect = activePrompt.GetComponent<RectTransform>();
            }
            else
            {
                CreateDefaultPrompt();
            }

            activePrompt.SetActive(false);
        }

        private void CreateDefaultPrompt()
        {
            // Crear GameObject raíz
            activePrompt = new GameObject("PickupPrompt");
            activePrompt.transform.SetParent(transform);
            activePrompt.transform.localPosition = Vector3.zero;
            activePrompt.transform.localRotation = Quaternion.identity;

            promptRect = activePrompt.AddComponent<RectTransform>();
            promptRect.sizeDelta = new Vector2(200f, 60f);

            // Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(activePrompt.transform);
            bgObj.transform.localPosition = Vector3.zero;
            bgObj.transform.localRotation = Quaternion.identity;

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            promptBackground = bgObj.AddComponent<Image>();
            promptBackground.color = backgroundColor;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(activePrompt.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            promptTextMesh = textObj.AddComponent<TextMeshProUGUI>();
            promptTextMesh.text = promptText;
            promptTextMesh.fontSize = 24;
            promptTextMesh.color = textColor;
            promptTextMesh.alignment = TextAlignmentOptions.Center;
            promptTextMesh.fontStyle = FontStyles.Bold;
        }

        #endregion

        #region Treasure Detection

        private void FindNearestTreasure()
        {
            if (diverTransform == null) return;

            // Buscar tesoros en el radio
            Collider2D[] colliders = Physics2D.OverlapCircleAll(
                diverTransform.position,
                detectionRadius,
                treasureLayer
            );

            float nearestDistance = float.MaxValue;
            LootObject nearest = null;

            foreach (Collider2D col in colliders)
            {
                LootObject loot = col.GetComponent<LootObject>();

                if (loot == null) continue;
                if (loot.IsGrabbed()) continue; // Ignorar si ya está agarrado

                float distance = Vector2.Distance(diverTransform.position, col.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = loot;
                }
            }

            nearestTreasure = nearest;
        }

        #endregion

        #region Prompt Update

        private void UpdatePrompt()
        {
            if (nearestTreasure == null)
            {
                HidePrompt();
                return;
            }

            // Verificar que la bolsa puede almacenar
            if (diverBag != null && diverBag.IsFull())
            {
                HidePrompt();
                return;
            }

            ShowPrompt();
            UpdatePromptPosition();

            if (animatePrompt)
            {
                AnimatePrompt();
            }
        }

        private void ShowPrompt()
        {
            if (activePrompt == null)
            {
                CreatePrompt();
            }

            if (!activePrompt.activeSelf)
            {
                activePrompt.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (activePrompt != null && activePrompt.activeSelf)
            {
                activePrompt.SetActive(false);
            }
        }

        private void UpdatePromptPosition()
        {
            if (nearestTreasure == null) return;

            Vector3 targetPos = nearestTreasure.transform.position + Vector3.up * promptHeight;
            transform.position = targetPos;
            basePosition = targetPos;

            // Hacer que mire a la cámara
            if (Camera.main != null)
            {
                transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                                Camera.main.transform.rotation * Vector3.up);
            }
        }

        private void AnimatePrompt()
        {
            bounceTimer += Time.deltaTime * bounceSpeed;

            float bounceOffset = Mathf.Sin(bounceTimer) * bounceAmplitude;
            transform.position = basePosition + Vector3.up * bounceOffset;
        }

        #endregion

        #region Input Handling

        private void CheckInput()
        {
            if (nearestTreasure == null) return;
            if (diverBag == null) return;

            bool inputPressed = false;

            // Keyboard
            if (Input.GetKeyDown(pickupKey))
            {
                inputPressed = true;
            }

            // Input System (si está configurado)
            // TODO: Integrar con AbyssalReachControls.DiverControls.Interact

            if (inputPressed)
            {
                PickupTreasure();
            }
        }

        #endregion

        #region Pickup

        private void PickupTreasure()
        {
            if (nearestTreasure == null) return;
            if (diverBag == null) return;

            if (showDebug)
            {
                Debug.Log("[Prompt] Recogiendo tesoro: " + nearestTreasure.GetItemData().itemName);
            }

            // Almacenar en bolsa (teleport)
            diverBag.StoreTreasure(nearestTreasure);

            // Limpiar referencia
            nearestTreasure = null;
            HidePrompt();
        }

        #endregion

        #region Public API

        public void SetPromptText(string text)
        {
            promptText = text;
            if (promptTextMesh != null)
            {
                promptTextMesh.text = text;
            }
        }

        public void SetDetectionRadius(float radius)
        {
            detectionRadius = radius;
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showDebug) return;
            if (diverTransform == null) return;

            // Radio de detección
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(diverTransform.position, detectionRadius);

            // Línea al tesoro más cercano
            if (nearestTreasure != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(diverTransform.position, nearestTreasure.transform.position);
            }
        }

        #endregion
    }
}