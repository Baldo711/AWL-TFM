namespace AccessWatchLite.Domain;

/// <summary>
/// Perfil de comportamiento habitual de un usuario
/// </summary>
public sealed class UserProfile
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserPrincipalName { get; set; }
    
    // Comportamiento habitual (JSON arrays)
    public string? UsualCountries { get; set; }
    public string? UsualCities { get; set; }
    public string? UsualIpRanges { get; set; }
    public string? KnownDevices { get; set; }
    public string? UsualSchedule { get; set; }
    public string? UsualAuthMethods { get; set; }
    public string? UsualClientApps { get; set; }
    
    // Estadísticas
    public int TotalAccessCount { get; set; }
    public int FailedAccessCount { get; set; }
    public DateTime? LastAccessDate { get; set; }
    
    // Configuración
    public bool IsHighPrivilege { get; set; }
    public decimal? CustomRiskThreshold { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
