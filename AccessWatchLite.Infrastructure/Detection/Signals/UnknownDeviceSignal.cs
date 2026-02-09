using AccessWatchLite.Application.Detection;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta accesos desde dispositivos no reconocidos
/// </summary>
public class UnknownDeviceSignal : ISignal
{
    private readonly DetectionConfig _config;
    
    public string Name => "Dispositivo Desconocido";
    public double Weight => _config.Weights.UnknownDevice;

    public UnknownDeviceSignal(DetectionConfig config)
    {
        _config = config;
    }

    public Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        var deviceId = currentEvent.DeviceId;
        
        // Si no hay DeviceId, no podemos evaluar
        if (string.IsNullOrEmpty(deviceId))
        {
            return Task.FromResult(new SignalResult(
                Score: 0.3, // Riesgo leve por falta de info
                IsTriggered: true,
                Description: "DeviceId no disponible"
            ));
        }
        
        // Si no hay perfil suficiente, no disparar señal fuerte
        if (profile.TotalAccessCount < _config.MinimumAccessesForProfile)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "Perfil insuficiente para evaluar dispositivo"
            ));
        }

        // Verificar si el dispositivo es conocido
        var isKnownDevice = profile.KnownDevices.Contains(deviceId);
        
        if (isKnownDevice)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Dispositivo reconocido: {deviceId.Substring(0, Math.Min(8, deviceId.Length))}..."
            ));
        }
        
        // Dispositivo completamente nuevo (riesgo alto)
        return Task.FromResult(new SignalResult(
            Score: 1.0,
            IsTriggered: true,
            Description: $"Dispositivo no reconocido: {deviceId.Substring(0, Math.Min(8, deviceId.Length))}... Total dispositivos conocidos: {profile.KnownDevices.Count}"
        ));
    }
}
