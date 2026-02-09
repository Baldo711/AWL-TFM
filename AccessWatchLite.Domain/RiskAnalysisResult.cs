namespace AccessWatchLite.Domain;

/// <summary>
/// Resultado del análisis de riesgo de un evento
/// </summary>
public sealed class RiskAnalysisResult
{
    public Guid EventId { get; set; }
    public decimal TotalRiskScore { get; set; }
    public string Severity { get; set; } = "Low"; // Low, Medium, High
    public List<RiskSignal> DetectedSignals { get; set; } = new();
    public bool RequiresAlert { get; set; }
    public string? RecommendedAction { get; set; }
}
