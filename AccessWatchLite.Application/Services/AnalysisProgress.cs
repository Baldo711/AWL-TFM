namespace AccessWatchLite.Application.Services;

/// <summary>
/// DTO para reportar progreso de análisis
/// </summary>
public class AnalysisProgress
{
    public bool IsRunning { get; set; }
    public int TotalEvents { get; set; }
    public int ProcessedEvents { get; set; }
    public int AlertsCreated { get; set; }
    public DateTime? LastProcessedDate { get; set; }
    public string? LastProcessedUser { get; set; }
    public double ProgressPercentage => TotalEvents > 0 ? (ProcessedEvents * 100.0 / TotalEvents) : 0;
}
