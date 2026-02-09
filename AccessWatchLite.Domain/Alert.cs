namespace AccessWatchLite.Domain;

/// <summary>
/// Representa una alerta/incidente detectado por el motor de detección
/// </summary>
public sealed class Alert
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? UserId { get; set; }
    public string? UserPrincipalName { get; set; }
    
    // Clasificación
    public string Severity { get; set; } = string.Empty; // Low, Medium, High
    public decimal RiskScore { get; set; }
    public string Status { get; set; } = "New"; // New, Investigating, Resolved, FalsePositive
    
    // Detalles
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DetectedSignals { get; set; } // JSON array
    
    // Contexto del evento
    public DateTime EventTimestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DeviceId { get; set; }
    
    // Metadatos
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
    
    // Modo
    public bool IsSimulation { get; set; }
}

