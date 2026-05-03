using UnityEngine;
using static AbyssalReach.Gameplay.ropeVerlet;

namespace AbyssalReach.Gameplay
{
    // TetherSystem simplificado - Solo gestiona estado, upgrades y provee API
    // La física y visual la maneja RopeVerlet
    public class TetherSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform del barco (anclaje superior)")]
        [SerializeField] private Transform boatAnchor;

        [Tooltip("Transform del buceador (anclaje inferior)")]
        [SerializeField] private Transform diverAnchor;

        [Tooltip("Script de la cuerda Verlet")]
        [SerializeField] private ropeVerlet ropeVerlet;

        [Header("Tether Properties")]
        [Tooltip("Longitud máxima del cable en metros")]
        public float maxLength = 30f;

        [Tooltip("A partir de qué porcentaje empieza la tensión (0-1)")]
        [SerializeField] private float tensionThreshold = 0.9f;

        [Header("Debug")]
        [SerializeField] private bool showDebug = true;

        private float currentLength = 0f;
        private float tension = 0f;

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();

            // Sincronizar longitud inicial con la cuerda
            if (ropeVerlet != null)
            {
                ropeVerlet.SetMaxLength(maxLength);
            }
        }

        private void Update()
        {
            UpdateTetherState();
        }

        #endregion

        #region Setup

        private void ValidateReferences()
        {
            if (boatAnchor == null)
            {
                Debug.LogError("[TetherSystem] Boat Anchor no asignado");
            }

            if (diverAnchor == null)
            {
                Debug.LogError("[TetherSystem] Diver Anchor no asignado");
            }

            if (ropeVerlet == null)
            {
                ropeVerlet = GetComponent<ropeVerlet>();
                if (ropeVerlet == null)
                {
                    Debug.LogError("[TetherSystem] RopeVerlet no encontrado");
                }
            }
        }

        #endregion

        #region State Update

        private void UpdateTetherState()
        {
            if (boatAnchor == null || diverAnchor == null) return;

            // Calcular distancia real (en línea recta, no siguiendo la cuerda)
            currentLength = Vector2.Distance(boatAnchor.position, diverAnchor.position);

            // Calcular tensión basándose en la distancia
            float range = maxLength * (1f - tensionThreshold);
            float excessOverThreshold = currentLength - (maxLength * tensionThreshold);
            tension = Mathf.Clamp01(excessOverThreshold / range);

            if (showDebug)
            {
                Debug.Log($"[Tether] Length: {currentLength:F2}/{maxLength:F2} | Tension: {tension:F2}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Verifica si el cable está estirado al máximo
        /// </summary>
        public bool IsAtMaxLength()
        {
            return currentLength >= maxLength * 0.99f;
        }

        /// <summary>
        /// Obtiene la longitud actual del cable
        /// </summary>
        public float GetCurrentLength()
        {
            return currentLength;
        }

        /// <summary>
        /// Obtiene la longitud máxima del cable
        /// </summary>
        public float GetMaxLength()
        {
            return maxLength;
        }

        /// <summary>
        /// Obtiene el factor de tensión (0-1)
        /// </summary>
        public float GetTension()
        {
            return tension;
        }

        /// <summary>
        /// Mejora la longitud del cable
        /// </summary>
        public void UpgradeLength(float newLength)
        {
            if (newLength > maxLength)
            {
                maxLength = newLength;

                // Sincronizar con la cuerda
                if (ropeVerlet != null)
                {
                    ropeVerlet.SetMaxLength(maxLength);
                }

                if (showDebug)
                {
                    Debug.Log($"[TetherSystem] Cable upgraded to {newLength}m");
                }
            }
        }
        public void ReelInRope(float amount)
        {
            if (ropeVerlet != null)
            {
                ropeVerlet.ReelIn(amount);
            }
        }
        public void ResetTetherToMax()
        {
            if (ropeVerlet != null)
            {
                ropeVerlet.SetMaxLength(maxLength);
                ropeVerlet.ResetRope();
            }
        }

        /// <summary>
        /// Configura los anclajes
        /// </summary>
        public void SetAnchors(Transform boat, Transform diver)
        {
            boatAnchor = boat;
            diverAnchor = diver;
        }

        /// <summary>
        /// Obtiene el transform del buceador
        /// </summary>
        public Transform GetDiverAnchor()
        {
            return diverAnchor;
        }

        /// <summary>
        /// Obtiene el transform del barco
        /// </summary>
        public Transform GetBoatAnchor()
        {
            return boatAnchor;
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            if (!showDebug) return;

            // Círculo de rango máximo
            if (boatAnchor != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                DrawCircle(boatAnchor.position, maxLength, 30);
            }

            // Línea directa (para comparar con la cuerda)
            if (boatAnchor != null && diverAnchor != null)
            {
                Gizmos.color = IsAtMaxLength() ? Color.red : Color.yellow;
                Gizmos.DrawLine(boatAnchor.position, diverAnchor.position);
            }
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 previousPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 currentPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        } 

        #endregion
    }
}