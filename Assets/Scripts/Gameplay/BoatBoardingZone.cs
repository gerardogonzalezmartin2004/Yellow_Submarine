using UnityEngine;
using AbyssalReach.Core; 

namespace AbyssalReach.Gameplay
{
    // Este componente se coloca en un objeto hijo del Barco con un BoxCollider.
    // Detecta si el buceador entra en la zona y permite pulsar un botón para "subir a bordo".
    [RequireComponent(typeof(BoxCollider))]
    public class BoatBoardingZone : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Tag que debe tener el buceador para ser detectado")]
        [SerializeField] private string diverTag = "Diver";

        [Header("UI Message")]
        [Tooltip("Mensaje que aparece en pantalla cuando puedes subir")]
        [SerializeField] private string boardingMessage = "Press 'Jump' to Board Boat";

        [Header("Debug")]
        [Tooltip("Muestra el collider y el mensaje en el editor")]
        [SerializeField] private bool showDebug = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.3f); // Amarillo transparente

        
        private bool diverInRange = false;

        // Referencia a los controles 
        private AbyssalReachControls controls;

        #region Unity cilclo de vida

        private void Awake()
        {
            // Inicializamos los controles
            controls = new AbyssalReachControls();

           // Aseguramos que el collider es un trigger, por si
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            controls.Enable();

            // Habilitamos el mapa de controles del buceador, ya que es quien interactúa
            controls.DiverControls.Enable();

            
            // Suscribimos la acción de "Ascender" (o saltar) para subir al barco
            controls.DiverControls.Ascend.performed += OnBoardPressed;
        }

        private void OnDisable()
        {
            // Limpieza de eventos 
            controls.DiverControls.Ascend.performed -= OnBoardPressed;

            controls.DiverControls.Disable();
            controls.Disable();
        }

        #endregion

        #region Input Logic

        // Este método se llama automáticamente cuando el jugador pulsa el botón
        private void OnBoardPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Solo hacemos algo si el buzo está realmente cerca del barco
            if (diverInRange)
            {
                BoardTheBoat();
            }
        }

        #endregion

        #region Trigger Detection

        // Se llama cuando algo entra en la zona
        private void OnTriggerEnter(Collider other)
        {
            // Verificamos si ese algo es el buceador (el algo es other, pero por si)
            if (other.CompareTag(diverTag))
            {
                diverInRange = true;
                
            }
        }

        // Se llama cuando el buceador sale de la zona
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(diverTag))
            {
                diverInRange = false;

            }
        }

        #endregion

        #region Game Logic

        private void BoardTheBoat()
        {
            
            // Llamamos al GameController (Singleton) para cambiar el estado del juego
            if (GameController.Instance != null)
            {
                // Esta función se encarga de apagar al buzo, encender al barco y cambiar la cámara
                GameController.Instance.EndDive();
            }
            else
            {
                Debug.LogError("[BoardingZone] GameController.Instanceno se ha encontrado");
            }
        }

        #endregion

        #region Debug (Gizmos)

        // Dibuja el cubo amarillo en la escena 
        private void OnDrawGizmos()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                // Usamos la matriz del objeto para que el cubo rote y se escale con el barco
                Gizmos.matrix = transform.localToWorldMatrix;

                Gizmos.color = gizmoColor;
                // Dibujamos el cubo relleno
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
        }

        // Dibuja el contorno amarillo cuando seleccionas el objeto
        private void OnDrawGizmosSelected()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = Color.yellow;
                // Dibujamos solo las líneas del borde
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
        }

        // Dibuja el mensaje "Press Space..." en la pantalla del juego
        private void OnGUI()
        {
            // Solo dibujamos si el debug está activo Y el buzo está cerca
            if (!showDebug || !diverInRange)
            {
                return;
            }

            // Configuración del estilo del texto
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            // Calculamos la posición (centro inferior de la pantalla)
            float width = 500;
            float height = 40;
            Rect rect = new Rect((Screen.width - width) / 2,Screen.height - 150,width,height);

            // Caja negra semitransparente de fondo para que se lea mejor. Al fnal todos estos apartado es para ir mas rapido, ya que ya introduciremos nuestro sistema de UI propio, pero por ahora esto nos sirve para testear la zona de abordaje sin tener que crear nada mas.
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(rect, "");

            // Texto blanco encima
            GUI.color = Color.white;
            GUI.Label(rect, boardingMessage, style);
        }

        #endregion
    }
}