using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Services;

/// <summary>
/// Servicio para obtener datos del Dashboard
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Obtiene los últimos accesos (eventos) registrados
    /// </summary>
    Task<List<AccessEvent>> GetRecentAccessesAsync(bool isSimulation, int count = 5, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene eventos de acceso para consulta filtrable
    /// </summary>
    Task<List<AccessEvent>> GetAccessEventsAsync(bool isSimulation, int count = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Obtiene un resumen de alertas por severidad
    /// </summary>
    Task<AlertsSummary> GetAlertsSummaryAsync(bool isSimulation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumen de alertas por severidad
/// </summary>
public sealed record AlertsSummary(
    int HighCount,
    int MediumCount,
    int LowCount,
    int TotalCount
);
