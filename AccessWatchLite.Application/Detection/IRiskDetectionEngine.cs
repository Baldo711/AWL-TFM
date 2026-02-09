using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Detection;

/// <summary>
/// Motor de detección de accesos anómalos basado en señales de riesgo y agregación ponderada
/// Implementa el modelo descrito en el TFM: Riesgo = ?(Señal_i × Peso_i)
/// </summary>
public interface IRiskDetectionEngine
{
    /// <summary>
    /// Analiza un evento de acceso y retorna un Alert si supera los umbrales configurados
    /// </summary>
    Task<Alert?> AnalyzeEventAsync(AccessEvent accessEvent, bool isSimulation, CancellationToken cancellationToken = default);
}
