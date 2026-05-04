using UnityEngine;

// Strategy Pattern: Interfaz para diferentes estrategias de retorno del item.
// Permite cambiar el comportamiento de "cómo vuelve el item" sin modificar el código principal.
public interface IReturnStrategy
{
    // Ejecuta el retorno del item a su última posición.


    bool ExecuteReturn(InventoryItem item, ItemMemento memento);


    string StrategyName { get; }
}

// Retorno instantáneo 
// El item aparece inmediatamente en su posición original.
public class InstantReturnStrategy : IReturnStrategy
{
    public string StrategyName => "Instant Return";

    public bool ExecuteReturn(InventoryItem item, ItemMemento memento)
    {
        if (item == null || memento == null || !memento.IsValid)
        {
            Debug.LogWarning("[InstantReturnStrategy] Parámetros inválidos");
            return false;
        }

        // Restaurar directamente usando el memento
        bool restored = memento.RestoreItem(item);



        return restored;
    }
}

//  Retorno animado con Lerp.
// El item se mueve suavemente a su posición original.
public class LerpReturnStrategy : IReturnStrategy
{
    public string StrategyName => "Lerp Return";

    private float duration = 0.3f; // Duración de la animación en segundos

    public LerpReturnStrategy(float animationDuration = 0.3f)
    {
        this.duration = animationDuration;
    }

    public bool ExecuteReturn(InventoryItem item, ItemMemento memento)
    {
        if (item == null || memento == null || !memento.IsValid)
        {
            Debug.LogWarning("[LerpReturnStrategy] Parámetros inválidos");
            return false;
        }

        // Iniciar corrutina de animación en el ItemPositionMemory, pero ya luego


        Debug.Log($"[LerpReturnStrategy] Iniciando lerp a ({memento.GridX}, {memento.GridY}) en {duration}s");

        // Por ahora, hacemos el retorno instantáneo
        // La implementación completa de la animación la haremos en ItemPositionMemory
        return memento.RestoreItem(item);
    }

    public float GetDuration() => duration;
}

//  Retorno con efecto de rebote
// El item vuelve con un efecto elástico.
public class BounceReturnStrategy : IReturnStrategy
{
    public string StrategyName => "Bounce Return";

    private float duration = 0.5f;
    private float bounceAmount = 1.2f; // Overshoot del bounce

    public BounceReturnStrategy(float animationDuration = 0.5f, float bounce = 1.2f)
    {
        this.duration = animationDuration;
        this.bounceAmount = bounce;
    }

    public bool ExecuteReturn(InventoryItem item, ItemMemento memento)
    {
        if (item == null || memento == null || !memento.IsValid)
        {
            Debug.LogWarning("[BounceReturnStrategy] Parámetros inválidos");
            return false;
        }

        Debug.Log($"[BounceReturnStrategy] Iniciando bounce a ({memento.GridX}, {memento.GridY})");

        // Implementación completa en ItemPositionMemory
        return memento.RestoreItem(item);
    }

    public float GetDuration() => duration;
    public float GetBounceAmount() => bounceAmount;
}

//Factory para crear estrategias de retorno
public static class ReturnStrategyFactory
{
    public enum StrategyType
    {
        Instant,
        Lerp,
        Bounce
    }

    // Crea una estrategia según el tipo especificado.
    public static IReturnStrategy CreateStrategy(StrategyType type)
    {
        switch (type)
        {
            case StrategyType.Instant:
                return new InstantReturnStrategy();

            case StrategyType.Lerp:
                return new LerpReturnStrategy(0.3f);

            case StrategyType.Bounce:
                return new BounceReturnStrategy(0.5f, 1.2f);

            default:
                return new InstantReturnStrategy();
        }
    }
}