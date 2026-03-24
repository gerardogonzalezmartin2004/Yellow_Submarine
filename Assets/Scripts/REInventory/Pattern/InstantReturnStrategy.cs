using UnityEngine;

/// <summary>
/// Strategy Pattern: Interfaz para diferentes estrategias de retorno del item.
/// Permite cambiar el comportamiento de "cómo vuelve el item" sin modificar el código principal.
/// </summary>
public interface IReturnStrategy
{
    /// <summary>
    /// Ejecuta el retorno del item a su última posición.
    /// </summary>
    /// <param name="item">Item a retornar</param>
    /// <param name="memento">Memento con la posición de destino</param>
    /// <returns>True si el retorno fue exitoso</returns>
    bool ExecuteReturn(InventoryItem item, ItemMemento memento);

    /// <summary>
    /// Nombre de la estrategia (para debugging).
    /// </summary>
    string StrategyName { get; }
}

/// <summary>
/// Estrategia: Retorno instantáneo (sin animación).
/// El item aparece inmediatamente en su posición original.
/// </summary>
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

        if (restored)
        {
            Debug.Log($"[InstantReturnStrategy] Item retornado instantáneamente a ({memento.GridX}, {memento.GridY})");
        }

        return restored;
    }
}

/// <summary>
/// Estrategia: Retorno animado con Lerp.
/// El item se mueve suavemente a su posición original.
/// </summary>
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

        // Iniciar corrutina de animación en el ItemPositionMemory
        // (lo haremos más adelante en ItemPositionMemory.cs)

        Debug.Log($"[LerpReturnStrategy] Iniciando lerp a ({memento.GridX}, {memento.GridY}) en {duration}s");

        // Por ahora, hacemos el retorno instantáneo
        // La implementación completa de la animación la haremos en ItemPositionMemory
        return memento.RestoreItem(item);
    }

    public float GetDuration() => duration;
}

/// <summary>
/// Estrategia: Retorno con efecto de "rebote" (bounce).
/// El item vuelve con un efecto elástico.
/// </summary>
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

/// <summary>
/// Factory para crear estrategias de retorno.
/// </summary>
public static class ReturnStrategyFactory
{
    public enum StrategyType
    {
        Instant,
        Lerp,
        Bounce
    }

    /// <summary>
    /// Crea una estrategia según el tipo especificado.
    /// </summary>
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
                Debug.LogWarning($"[ReturnStrategyFactory] Tipo desconocido: {type}, usando Instant");
                return new InstantReturnStrategy();
        }
    }
}