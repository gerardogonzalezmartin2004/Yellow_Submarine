using UnityEngine;

namespace IA26.Movement.Kinematic
{
    public class KFlee : KSeek
    {


        /*
      private void Update()
      {
          SteeringOutput sOut = GetSteering();
          transform.Translate(sOut.linear * Time.deltaTime);

      }
      */
        /*
        private SteeringOutput GetSteering()
        {
            SteeringOutput sOut = new SteeringOutput();
            sOut.linear = transform.position - target.position;
            sOut.linear = sOut.linear.normalized * maxSpeed; //tb se puede poner -1 para que fuese hacia el toro lado
            return sOut;

        }
        */
        public override SteeringOutput GetSteering()
        {
            return base.GetSteering() * -1;
        }
    }
}

