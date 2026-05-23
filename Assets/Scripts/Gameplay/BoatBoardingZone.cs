using UnityEngine;
using AbyssalReach.Core; 

namespace AbyssalReach.Gameplay
{
    // Detecta si el buceador entra en la zona y permite pulsar un bot�n para "subir a bordo".
    [RequireComponent(typeof(BoxCollider2D))]
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
        [SerializeField] private Color gizmoColor = new Color(1f, 1f, 0f, 0.3f); 

        
        private bool diverInRange = false;

        // Referencia a los controles 
        private AbyssalReachControls controls;

        #region Unity cilclo de vida

        private void Awake()
        {
            // Inicializamos los controles
            controls = new AbyssalReachControls();

           // Aseguramos que el collider es un trigger, por si
            
            BoxCollider2D boxCollider2D = GetComponent<BoxCollider2D>();
            if (boxCollider2D != null)
            {
                boxCollider2D.isTrigger = true;
            }
        }

        private void OnEnable()
        {
            controls.Enable();

            // Habilitamos el mapa de controles del buceador, ya que es quien interact�a
            controls.DiverControls.Enable();

            
            // Suscribimos 
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

        // Este m�todo se llama autom�ticamente cuando el jugador pulsa el bot�n
        private void OnBoardPressed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            // Solo hacemos algo si el buzo est� realmente cerca del barco
            if (diverInRange)
            {
                BoardTheBoat();
            }
        }

        #endregion

        #region Trigger Detection

        // Se llama cuando algo entra en la zona
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Verificamos si ese algo es el buceador (el algo es other, pero por si)
            if (other.CompareTag(diverTag))
            {
                diverInRange = true;
                if (GameController.Instance != null && GameController.Instance.IsEmergencyAscent())
                {
                  
                    BoardTheBoat();
                }

            }
        }

        // Se llama cuando el buceador sale de la zona
        private void OnTriggerExit2D(Collider2D other)
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
                // Esta funci�n se encarga de apagar al buzo, encender al barco y cambiar la c�mara
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
                // Dibujamos solo las l�neas del borde
                Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            }
        }

        private void OnGUI()
        {
            if (!showDebug || !diverInRange) return;
            GUIStyle style = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.yellow;
            Rect rect = new Rect((Screen.width - 500) / 2, Screen.height - 100, 500, 40);
            GUI.color = new Color(0, 0, 0, 0.7f); GUI.Box(rect, ""); GUI.color = Color.white;
            GUI.Label(rect, boardingMessage, style);
        }

        #endregion
    }
}