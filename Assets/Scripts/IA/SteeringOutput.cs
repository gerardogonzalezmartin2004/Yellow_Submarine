using Engine = UnityEngine;
namespace IA26.Movement
{
    public struct SteeringOutput
    {
        public Engine.Vector2 linear;
        public float angular;

        public SteeringOutput(Engine.Vector2 linear, float angular)
        {
            this.linear = linear;
            this.angular = angular;
        }
        public static SteeringOutput operator +(SteeringOutput a, SteeringOutput b)
        {
            return new SteeringOutput(a.linear + b.linear, a.angular + b.angular);
        }

        public static SteeringOutput operator *(SteeringOutput a, float b)
        {
            return new SteeringOutput(a.linear * b, a.angular * b);
        }
    }
}

