namespace AccessWatchLite.Application.Detection;

/// <summary>
/// Configuración ajustable del motor de detección
/// </summary>
public class DetectionConfig
{
    // Umbrales de severidad (sobre 100)
    public double HighSeverityThreshold { get; set; } = 70.0;
    public double MediumSeverityThreshold { get; set; } = 40.0;
    public double MinimumAlertThreshold { get; set; } = 30.0;
    
    // Periodo de baseline (días hacia atrás)
    public int ProfileLookbackDays { get; set; } = 30;
    
    // Número mínimo de accesos para construir perfil confiable
    public int MinimumAccessesForProfile { get; set; } = 10;
    
    // Umbrales por señal (0.0 - 1.0)
    public double UnusualLocationThreshold { get; set; } = 0.7;
    public double IpChangeThreshold { get; set; } = 0.6;
    public double UnknownDeviceThreshold { get; set; } = 0.8;
    public double AtypicalTimeThreshold { get; set; } = 0.5;
    public double WeakAuthThreshold { get; set; } = 0.6;
    
    // Configuración de intentos fallidos
    public int FailedAttemptsCount { get; set; } = 3;
    public int FailedAttemptsWindowMinutes { get; set; } = 15;
    
    // Pesos de señales (deben sumar ~1.0 para normalización)
    public SignalWeights Weights { get; set; } = new();
}

/// <summary>
/// Pesos de cada señal en el cálculo agregado de riesgo
/// </summary>
public class SignalWeights
{
    public double UnusualLocation { get; set; } = 0.20;
    public double IpChange { get; set; } = 0.15;
    public double UnknownDevice { get; set; } = 0.25;
    public double AtypicalTime { get; set; } = 0.10;
    public double WeakAuth { get; set; } = 0.15;
    public double FailedAttempts { get; set; } = 0.15;
    
    /// <summary>
    /// Suma total de pesos (debería ser ~1.0)
    /// </summary>
    public double Total => UnusualLocation + IpChange + UnknownDevice + 
                           AtypicalTime + WeakAuth + FailedAttempts;
}
