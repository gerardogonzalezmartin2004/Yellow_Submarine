using UnityEngine;

namespace IA26.Movement.Kinematic
{
    public class KSeek : MovementBehavior
    {

        [SerializeField] protected Transform target;
        [SerializeField] protected float maxSpeed;
        /*
        private void Update()
        {
            SteeringOutput sOut = GetSteering();
            transform.Translate(sOut.linear * Time.deltaTime);

        }
        */
        public override SteeringOutput GetSteering()
        {
            SteeringOutput sOut = new SteeringOutput();
            sOut.linear = target.position - transform.position;
            sOut.linear = sOut.linear.normalized * maxSpeed;
            return sOut;


        }
    }
}
