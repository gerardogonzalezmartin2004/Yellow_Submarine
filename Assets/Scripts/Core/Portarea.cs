using UnityEngine;

namespace AbyssalReach.Gameplay
{
    // Área del puerto que detecta cuando el barco está cerca.
    // Muestra UI de interacción y permite abrir la tienda.
    //
    // SETUP:
    // 1. Crear GameObject vacío llamado "Port"
    // 2. Añadir este script
    // 3. Configurar el radio de interacción
    // 4. El Gizmo mostrará el área en el editor
    public class PortArea : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Radio de detección para activar la tienda")]
        [SerializeField] private float interactionRadius = 5f;

        [Tooltip("Tag del objeto que debe entrar (normalmente 'Player' o 'Boat')")]
        [SerializeField] private string targetTag = "Boat";

        [Header("UI Message")]
        [Tooltip("Mensaje mostrado cuando el barco está en rango")]
        [SerializeField] private string interactionMessage = "Press 'E' to Open Shop";

        [Header("References")]
        [SerializeField] private GameObject shopUIPanel;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

        // State
        private bool playerInRange = false;
        private GameObject detectedBoat;

        #region Unity Lifecycle

        private void Update()
        {
            CheckForBoatInRange();
            HandleInput();
        }

        #endregion

        #region Detection Logic

        private void CheckForBoatInRange()
        {
            // Buscar el barco por tag
            GameObject boat = GameObject.FindGameObjectWithTag(targetTag);

            if (boat == null)
            {
                playerInRange = false;
                detectedBoat = null;
                return;
            }

            // Calcular distancia
            float distance = Vector3.Distance(transform.position, boat.transform.position);

            // Verificar si está en rango
            bool wasInRange = playerInRange;
            playerInRange = distance <= interactionRadius;

            // Detectar entrada/salida
            if (playerInRange && !wasInRange)
            {
                OnBoatEnterRange(boat);
            }
            else if (!playerInRange && wasInRange)
            {
                OnBoatExitRange();
            }

            if (playerInRange)
            {
                detectedBoat = boat;
            }
            else
            {
                detectedBoat = null;
            }
        }

        private void OnBoatEnterRange(GameObject boat)
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Boat entered port area");
            }

            // Notificar al GameController
            if (Core.GameController.Instance != null)
            {
                Core.GameController.Instance.EnterPort();
            }
        }

        private void OnBoatExitRange()
        {
            if (showDebug)
            {
                Debug.Log("[PortArea] Boat left port area");
            }

            // Cerrar shop si estaba abierta
            if (shopUIPanel != null && shopUIPanel.activeSelf)
            {
                CloseShop();
            }

            // Notificar al GameController
            if (Core.GameController.Instance != null)
            {
                Core.GameController.Instance.ExitPort();
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            if (!playerInRange)
            {
                return;
            }

            // Detectar input para abrir tienda
            if (Input.GetKeyDown(KeyCode.E) )
            {
                ToggleShop();
            }
        }

        #endregion

        #region Shop Control

        private void ToggleShop()
        {
            if (shopUIPanel == null)
            {
                Debug.LogWarning("[PortArea] Shop UI Panel not assigned!");
                return;
            }

            if (shopUIPanel.activeSelf)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }

        private void OpenShop()
        {
            if (shopUIPanel != null)
            {
                shopUIPanel.SetActive(true);

                if (showDebug)
                {
                    Debug.Log("[PortArea] Shop opened");
                }
            }
        }

        private void CloseShop()
        {
            if (shopUIPanel != null)
            {
                shopUIPanel.SetActive(false);

                if (showDebug)
                {
                    Debug.Log("[PortArea] Shop closed");
                }
            }
        }

        #endregion

        #region Gizmos & Debug

        private void OnDrawGizmos()
        {
            // Dibujar radio de interacción
            Gizmos.color = gizmoColor;
            DrawWireCircle(transform.position, interactionRadius, 32);

            // Dibujar esfera sólida en el centro
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.6f);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            // Dibujar círculo más visible cuando está seleccionado
            Gizmos.color = Color.green;
            DrawWireCircle(transform.position, interactionRadius, 64);

            // Texto del radio
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * interactionRadius,
                "Port Radius: " + interactionRadius.ToString("F1") + "m"
            );
        }

        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3( Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }

        private void OnGUI()
        {
            if (!showDebug || !playerInRange)
            {
                return;
            }

            // Mostrar mensaje de interacción
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            float width = 400;
            float height = 40;
            Rect rect = new Rect(
                (Screen.width - width) / 2,
                Screen.height - 100,
                width,
                height
            );

            // Fondo semitransparente
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, "");
            GUI.color = Color.white;

            // Texto
            GUI.Label(rect, interactionMessage, style);
        }

        #endregion
    }
}