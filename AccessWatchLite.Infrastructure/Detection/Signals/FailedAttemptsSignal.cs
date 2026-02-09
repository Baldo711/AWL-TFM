using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta múltiples intentos fallidos previos (posible brute force)
/// </summary>
public class FailedAttemptsSignal : ISignal
{
    private readonly DetectionConfig _config;
    private readonly IAccessEventRepository _accessEventRepository;
    
    public string Name => "Múltiples Intentos Fallidos";
    public double Weight => _config.Weights.FailedAttempts;

    public FailedAttemptsSignal(DetectionConfig config, IAccessEventRepository accessEventRepository)
    {
        _config = config;
        _accessEventRepository = accessEventRepository;
    }

    public async Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        // Validar que UserId no sea null
        if (string.IsNullOrEmpty(currentEvent.UserId))
        {
            return new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "UserId no disponible"
            );
        }

        // Verificar intentos fallidos recientes para este usuario
        var windowStart = DateTime.UtcNow.AddMinutes(-_config.FailedAttemptsWindowMinutes);
        
        var failedCount = await _accessEventRepository.CountFailedAttemptsAsync(
            currentEvent.UserId, 
            isSimulation: false, // Solo datos REAL
            since: windowStart, 
            cancellationToken);
        
        if (failedCount == 0)
        {
            return new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Sin intentos fallidos en los últimos {_config.FailedAttemptsWindowMinutes} minutos"
            );
        }
        
        if (failedCount < _config.FailedAttemptsCount)
        {
            // Hay fallos pero bajo el umbral (riesgo leve)
            return new SignalResult(
                Score: 0.3,
                IsTriggered: false,
                Description: $"{failedCount} intento(s) fallido(s) en los últimos {_config.FailedAttemptsWindowMinutes} minutos"
            );
        }
        
        // Múltiples intentos fallidos detectados
        // Score proporcional al número de intentos (máximo 10 intentos = score 1.0)
        var score = Math.Min(failedCount / 10.0, 1.0);
        
        // Si el evento actual es exitoso después de múltiples fallos, es MUY sospechoso
        if (currentEvent.Result == "CORRECTO")
        {
            return new SignalResult(
                Score: 1.0,
                IsTriggered: true,
                Description: $"Acceso exitoso tras {failedCount} intentos fallidos en {_config.FailedAttemptsWindowMinutes} minutos (posible compromiso de credenciales)"
            );
        }
        
        // Evento fallido en medio de otros fallos (posible ataque en curso)
        return new SignalResult(
            Score: score,
            IsTriggered: true,
            Description: $"{failedCount} intentos fallidos en los últimos {_config.FailedAttemptsWindowMinutes} minutos (posible ataque de fuerza bruta)"
        );
    }
}
