namespace AccessWatchLite.Domain;

/// <summary>
/// Representa una acción de respuesta ejecutada sobre una alerta
/// </summary>
public sealed class ResponseAction
{
    public Guid Id { get; set; }
    public Guid AlertId { get; set; }
    
    /// <summary>
    /// Tipo de acción: RevokeSession, BlockUser, RequireMfa, NotifyEmail, LogIncident
    /// </summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Estado: Pending, Executed, Failed
    /// </summary>
    public string ActionStatus { get; set; } = "Pending";
    
    /// <summary>
    /// Fecha de ejecución (null si aún no se ejecutó)
    /// </summary>
    public DateTime? ExecutedAt { get; set; }
    
    /// <summary>
    /// Resultado detallado de la ejecución (JSON)
    /// </summary>
    public string? Result { get; set; }
    
    /// <summary>
    /// Mensaje de error si falló
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Indica si es una acción en modo simulación
    /// </summary>
    public bool IsSimulation { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
