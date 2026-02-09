namespace AccessWatchLite.Application.Detection;

/// <summary>
/// Interfaz que representa una señal de riesgo individual
/// </summary>
public interface ISignal
{
    /// <summary>
    /// Nombre descriptivo de la señal
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Peso de la señal en el cálculo agregado (0.0 - 1.0)
    /// </summary>
    double Weight { get; }
    
    /// <summary>
    /// Evalúa la señal para un evento de acceso dado
    /// </summary>
    Task<SignalResult> EvaluateAsync(
        Domain.AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de la evaluación de una señal
/// </summary>
public record SignalResult(
    double Score,          // 0.0 = comportamiento normal, 1.0 = máxima anomalía
    bool IsTriggered,      // true si la señal detectó una anomalía
    string Description     // Explicación legible de por qué se disparó
);
