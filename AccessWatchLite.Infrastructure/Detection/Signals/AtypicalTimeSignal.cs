using AccessWatchLite.Application.Detection;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta accesos en horarios atípicos para el usuario
/// </summary>
public class AtypicalTimeSignal : ISignal
{
    private readonly DetectionConfig _config;
    
    public string Name => "Horario Atípico";
    public double Weight => _config.Weights.AtypicalTime;

    public AtypicalTimeSignal(DetectionConfig config)
    {
        _config = config;
    }

    public Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        // Si no hay perfil suficiente, no disparar señal
        if (profile.TotalAccessCount < _config.MinimumAccessesForProfile)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "Perfil insuficiente para evaluar horario"
            ));
        }

        var accessTime = currentEvent.CreatedAt;
        var dayOfWeek = accessTime.DayOfWeek;
        var timeOfDay = accessTime.TimeOfDay;
        
        // Verificar si hay horarios típicos para este día de la semana
        if (!profile.TypicalWorkingHours.TryGetValue(dayOfWeek, out var typicalRange))
        {
            // No hay datos para este día de la semana (riesgo moderado)
            return Task.FromResult(new SignalResult(
                Score: 0.5,
                IsTriggered: true,
                Description: $"Acceso en día no habitual: {dayOfWeek} a las {timeOfDay:hh\\:mm}"
            ));
        }
        
        // Verificar si el acceso está dentro del rango habitual
        if (typicalRange.Contains(timeOfDay))
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Horario habitual: {dayOfWeek} a las {timeOfDay:hh\\:mm}"
            ));
        }
        
        // Acceso fuera del horario habitual
        // Calcular qué tan lejos está del rango
        var distanceMinutes = CalculateDistanceFromRange(timeOfDay, typicalRange);
        
        // Score proporcional a la distancia (máximo 2 horas = score 1.0)
        var score = Math.Min(distanceMinutes / 120.0, 1.0);
        
        return Task.FromResult(new SignalResult(
            Score: score,
            IsTriggered: score >= _config.AtypicalTimeThreshold,
            Description: $"Horario inusual: {dayOfWeek} a las {timeOfDay:hh\\:mm}. Rango habitual: {typicalRange.Start:hh\\:mm}-{typicalRange.End:hh\\:mm}"
        ));
    }
    
    private static double CalculateDistanceFromRange(TimeSpan time, TimeRange range)
    {
        if (time < range.Start)
        {
            return (range.Start - time).TotalMinutes;
        }
        else if (time > range.End)
        {
            return (time - range.End).TotalMinutes;
        }
        return 0;
    }
}
