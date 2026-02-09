namespace AccessWatchLite.Application.Services;

public class AnalysisRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? BatchSize { get; set; }
}

public class AnalysisResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EventsProcessed { get; set; }
    public int AlertsCreated { get; set; }
    public Dictionary<string, int> AlertsBySeverity { get; set; } = new();
    public double DurationSeconds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Servicio para interactuar con la Azure Function de análisis
/// </summary>
public interface ISimAnalysisService
{
    Task<AnalysisResult> TriggerAnalysisAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
    Task<(DateTime? min, DateTime? max)> GetDateRangeAsync(CancellationToken cancellationToken = default);
    Task<(bool hasData, (DateTime? min, DateTime? max) dateRange)> HasSimulationDataAsync(CancellationToken cancellationToken = default);
    Task<AnalysisProgress> GetAnalysisProgressAsync(CancellationToken cancellationToken = default);
}
