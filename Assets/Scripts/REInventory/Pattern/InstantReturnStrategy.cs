using UnityEngine;


public interface IReturnStrategy
{
   
    bool ExecuteReturn(InventoryItem item, ItemMemento memento);


    string StrategyName { get; }
}


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


public class LerpReturnStrategy : IReturnStrategy
{
    public string StrategyName => "Lerp Return";

    private float duration = 0.3f; 

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

      


        Debug.Log($"[LerpReturnStrategy] Iniciando lerp a ({memento.GridX}, {memento.GridY}) en {duration}s");

    
        return memento.RestoreItem(item);
    }

    public float GetDuration() => duration;
}


// El item vuelve con un efecto elástico.
public class BounceReturnStrategy : IReturnStrategy
{
    public string StrategyName => "Bounce Return";

    private float duration = 0.5f;
    private float bounceAmount = 1.2f; 

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

     
        return memento.RestoreItem(item);
    }

    public float GetDuration() => duration;
    public float GetBounceAmount() => bounceAmount;
}

public static class ReturnStrategyFactory
{
    public enum StrategyType
    {
        Instant,
        Lerp,
        Bounce
    }

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