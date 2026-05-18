using UnityEngine;
namespace IA26.Movement.Dinamic
{
    public class DSeparation : MovementBehavior
    {
        [SerializeField] private Rigidbody2D agente;
        [SerializeField] private float aceleracionMaxima = 5f;
        [SerializeField] private float radioDeSatisfaccion = 2f;
        [SerializeField] private float damping = 0.9f; // ← frena la velocidad residual
        [SerializeField] private LayerMask layerMask;  // ← solo detecta lo que nos interesa

        public override SteeringOutput GetSteering()
        {
            SteeringOutput sOut = new SteeringOutput();

            Collider2D[] obstaculos = Physics2D.OverlapCircleAll(
                agente.position, radioDeSatisfaccion, layerMask);

            foreach (Collider2D obstaculo in obstaculos)
            {
                // ── FIX 1: ignorarse a sí mismo ──
                if (obstaculo.gameObject == gameObject) continue;

                Vector2 direccion = agente.position - (Vector2)obstaculo.transform.position;
                float distancia = direccion.magnitude;

                // ── FIX 2: evitar división por cero si están en la misma posición ──
                if (distancia < 0.001f) continue;

                if (distancia < radioDeSatisfaccion)
                {
                    float fuerza = aceleracionMaxima *
                                   (radioDeSatisfaccion - distancia) / radioDeSatisfaccion;
                    sOut.linear += fuerza * direccion.normalized;
                }
            }

            if (sOut.linear.magnitude > aceleracionMaxima)
                sOut.linear = sOut.linear.normalized * aceleracionMaxima;

            return sOut;
        }

        private void Update()
        {
            SteeringOutput sOut = GetSteering();
            agente.linearVelocity += sOut.linear * Time.deltaTime;

            // ── FIX 3: damping — si no hay separación activa, frena ──
            if (sOut.linear.magnitude < 0.01f)
                agente.linearVelocity *= damping;
        }
    }
}