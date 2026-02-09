using AccessWatchLite.Application.Detection;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta cambios bruscos de dirección IP
/// </summary>
public class IpChangeSignal : ISignal
{
    private readonly DetectionConfig _config;
    
    public string Name => "Cambio de IP";
    public double Weight => _config.Weights.IpChange;

    public IpChangeSignal(DetectionConfig config)
    {
        _config = config;
    }

    public Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        var currentIp = currentEvent.IpAddress ?? "0.0.0.0";
        
        // Si no hay perfil suficiente, no disparar señal
        if (profile.TotalAccessCount < _config.MinimumAccessesForProfile)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "Perfil insuficiente para evaluar IP"
            ));
        }

        // Verificar si la IP está en la lista de IPs habituales
        var isKnownIp = profile.CommonIps.Contains(currentIp);
        
        if (isKnownIp)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"IP habitual: {currentIp}"
            ));
        }
        
        // IP no reconocida (riesgo medio-alto)
        // Si hay muchas IPs habituales, es menos sospechoso (usuario móvil)
        var score = profile.CommonIps.Count > 10 ? 0.6 : 0.8;
        
        return Task.FromResult(new SignalResult(
            Score: score,
            IsTriggered: true,
            Description: $"IP no habitual: {currentIp}. IPs conocidas: {profile.CommonIps.Count}"
        ));
    }
}
