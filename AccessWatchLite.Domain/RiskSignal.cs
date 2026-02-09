namespace AccessWatchLite.Domain;

/// <summary>
/// Representa una señal de riesgo detectada
/// </summary>
public sealed class RiskSignal
{
    public string SignalName { get; set; } = string.Empty;
    public decimal Value { get; set; } // Normalizado 0-1
    public decimal Weight { get; set; }
    public string? Description { get; set; }
}
