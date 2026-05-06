using IA26.Movement;
using IA26.Movement.Combination;
using UnityEngine;
namespace IA.Movement.Combination
{
    public class BlendedSteering : MonoBehaviour
    {
        [SerializeField] private BehaviorAndWeight[] behaviors;

        [SerializeField] private float maxAcceleration;
        [SerializeField] private float maxAngularAceleration;



        private void Update()
        {
            SteeringOutput sOut = GetSteering();
            transform.Translate(sOut.linear * Time.deltaTime);
        }
        public SteeringOutput GetSteering()
        {
            SteeringOutput sOut = new SteeringOutput();
            foreach (BehaviorAndWeight behavior in behaviors)
            {

                SteeringOutput steering = behavior.behavior.GetSteering();
                sOut.linear += steering.linear * behavior.weight;
                sOut.angular += steering.angular * behavior.weight;

                // sOut += behavior.behavior.GetSteering() * behavior.weight;

            }
            sOut.linear = Vector2.Min(sOut.linear, sOut.linear.normalized * maxAcceleration);
            sOut.angular = Mathf.Min(sOut.angular, maxAngularAceleration);


            return sOut;
        }

        public void SetWeight<T>(float weight) where T : MovementBehavior
        {
            for (int i = 0; i < behaviors.Length; i++)
            {
                if (behaviors[i].behavior.GetType() == typeof(T))
                {
                    behaviors[i].weight = weight;
                    return;
                }
            }
        }
    }

}



