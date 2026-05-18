using UnityEngine;
using System;

namespace IA26.Movement.Combination
{
    [Serializable]
    public struct BehaviorAndWeight
    {
        [Range(0, 1)] public float weight;
        public MovementBehavior behavior;


    }
}

