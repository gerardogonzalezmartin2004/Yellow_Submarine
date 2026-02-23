using UnityEngine;

namespace AbyssalReach.Gameplay
{
    // Responsable de renderizar el cable del gancho entre el buzo y el objeto enganchado.
    [RequireComponent(typeof(LineRenderer))]
    public class GrappleLineRenderer : MonoBehaviour
    {
        [Header("Line Settings")]
        [Tooltip("Ancho del cable en el origen")]
        [SerializeField] private float startWidth = 0.1f;

        [Tooltip("Ancho del cable en el destino")]
        [SerializeField] private float endWidth = 0.08f;

        [Tooltip("Material del cable")]
        [SerializeField] private Material cableMaterial;

        [Header("Visual Effects")]
        [Tooltip("Color base del cable")]
        [SerializeField] private Color cableColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Tooltip("Curvatura del cable (0 = recto, 1 = muy curvado)")]
        [SerializeField] private float sag = 0.5f;

        [Tooltip("Número de segmentos intermedios para la curva")]
        [SerializeField] private int curveSegments = 10;

        [Header("Performance")]
        [Tooltip("Si true, usa curva parabólica. Si false, línea recta")]
        [SerializeField] private bool useCurve = true;

        [Header("References")]
        [Tooltip("Punto de origen (Transform del buzo)")]
        [SerializeField] private Transform origin;

        [Tooltip("Punto de destino (Transform del objeto enganchado)")]
        [SerializeField] private Transform target;

        private LineRenderer lineRenderer;
        private Vector3[] linePoints;

        #region Unity Lifecycle

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }

        // Este se ejecuta despues de las fisicas, asegurando que los objetos hayan actualizado su posición
        private void LateUpdate()
        {
            UpdateLine();
        }

        #endregion

        #region Line Renderer Configuration

        private void ConfigureLineRenderer()
        {
            if (lineRenderer == null)
            {
                return;
            }

            // Configuración base
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            lineRenderer.material = cableMaterial;
            lineRenderer.startColor = cableColor;
            lineRenderer.endColor = cableColor;

            // Optimizaciones de rendering
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.useWorldSpace = true;

            // Suavizado visual de las uniones
            lineRenderer.numCornerVertices = 3;
            lineRenderer.numCapVertices = 3;
            lineRenderer.alignment = LineAlignment.TransformZ;
        }

        #endregion

        #region Line Update Logic

        private void UpdateLine()
        {
            // Validación dinámica: Asegura que el array y el LineRenderer tengan el tamaño correcto
            // Esto permite cambiar useCurve o curveSegments en el inspector sin romper el juego
            int requiredPoints = useCurve ? curveSegments + 2 : 2;
            if (lineRenderer == null || origin == null || target == null)
            {
                linePoints = new Vector3[requiredPoints];
                lineRenderer.positionCount = requiredPoints;
            }

            if (useCurve)
            {
                UpdateCurvedLine();
            }
            else
            {
                UpdateStraightLine();
            }
        }

        // Línea recta optimizada (2 puntos, sin usar el array completo para mayor velocidad)
        private void UpdateStraightLine()
        {
            lineRenderer.SetPosition(0, origin.position);
            lineRenderer.SetPosition(1, target.position);
        }

        // Línea con curva parabólica, simula peso/ gravedad del cable
        private void UpdateCurvedLine()
        {
            Vector3 start = origin.position;
            Vector3 end = target.position;

            // Calcular punto medio con offset de gravedad
            Vector3 mid = (start + end) * 0.5f;

            // Calcular la profundidad de la curva según la distancia
            float distance = Vector3.Distance(start, end);
            float sagAmount = sag * distance * 0.25f;

            // Aplicar offset hacia abajo para simular gravedad
            mid.y -= sagAmount;

            // Generar puntos de la curva usando interpolación cuadrática de Bézier
            for (int i = 0; i <= curveSegments + 1; i = i + 1)
            {
                float t = (float)i / (float)(curveSegments + 1);
                linePoints[i] = CalculateBezierPoint(t, start, mid, end);
            }

            lineRenderer.SetPositions(linePoints);
        }

        // Fórmula matemática de Bézier cuadrática (3 puntos de control)
        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;

            return (uu * p0) + (2f * u * t * p1) + (tt * p2);
        }

        #endregion

        #region Public API

        // Establece los Transform de inicio y fin para el renderizado del cable.
        public void SetTargets(Transform newOrigin, Transform newTarget)
        {
            origin = newOrigin;
            target = newTarget;
        }

        // Cambiar el color del cable 
        public void SetColor(Color color)
        {
            cableColor = color;

            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }

        // Activar/desactivar la curva
        public void SetCurveEnabled(bool enabled)
        {
            useCurve = enabled;
            // El array se redimensionará automáticamente en el próximo Update
        }

        
        // Muestra u oculta visualmente el cable del gancho.
        
        public void SetVisible(bool visible)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }

        #endregion

        #region Debug (Gizmos)

        private void OnDrawGizmos()
        {
            if (origin == null || target == null)
            {
                return;
            }

            // Dibujar línea de debug
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin.position, target.position);

            // Dibujar esferas en los extremos
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(origin.position, 0.1f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.1f);
        }

        #endregion
    }
}