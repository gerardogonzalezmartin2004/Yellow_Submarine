using UnityEngine;
namespace IA26.Movement
{
    public abstract class MovementBehavior : MonoBehaviour //todos los algoitmos de moviemento deverian heredar de esta clase, es una clase abstracta porque no se va a instanciar, solo se va a usar para heredar
    {
        public abstract SteeringOutput GetSteering();         //metodo abstracto que devuelve un SteeringOutput, cada algoritmo de movimiento va a implementar este metodo de manera diferente, es el metodo que se va a llamar para obtener el steering output de cada algoritmo de movimiento

    }
}
