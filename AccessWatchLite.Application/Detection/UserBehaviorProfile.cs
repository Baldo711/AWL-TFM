namespace AccessWatchLite.Application.Detection;

/// <summary>
/// Perfil de comportamiento construido desde el histórico de accesos
/// (usado para análisis de detección)
/// </summary>
public class UserBehaviorProfile
{
    public string UserId { get; set; } = string.Empty;
    
    // Ubicaciones habituales (top 5 más frecuentes)
    public List<string> CommonCountries { get; set; } = new();
    public List<string> CommonCities { get; set; } = new();
    
    // IPs habituales (últimas 20 únicas)
    public List<string> CommonIps { get; set; } = new();
    
    // Dispositivos reconocidos (DeviceIds únicos)
    public List<string> KnownDevices { get; set; } = new();
    
    // Horarios típicos de acceso (por día de semana)
    public Dictionary<DayOfWeek, TimeRange> TypicalWorkingHours { get; set; } = new();
    
    // Métodos de autenticación habituales
    public List<string> UsualAuthMethods { get; set; } = new();
    
    // Aplicaciones que suele usar
    public List<string> CommonApps { get; set; } = new();
    
    // Estadísticas
    public int TotalAccessCount { get; set; }
    public int SuccessfulAccessCount { get; set; }
    public int FailedAccessCount { get; set; }
    public DateTime ProfileBuiltAt { get; set; }
    public DateTime ProfilePeriodStart { get; set; }
    public DateTime ProfilePeriodEnd { get; set; }
}

/// <summary>
/// Rango de horas (para horarios típicos)
/// </summary>
public record TimeRange(TimeSpan Start, TimeSpan End)
{
    /// <summary>
    /// Verifica si una hora está dentro del rango
    /// </summary>
    public bool Contains(TimeSpan time)
    {
        if (Start <= End)
        {
            return time >= Start && time <= End;
        }
        else
        {
            // Caso que cruza medianoche (ej: 22:00 - 02:00)
            return time >= Start || time <= End;
        }
    }
}
