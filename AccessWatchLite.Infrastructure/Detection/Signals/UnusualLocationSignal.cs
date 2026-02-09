using AccessWatchLite.Application.Detection;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta accesos desde ubicaciones geográficas inusuales
/// </summary>
public class UnusualLocationSignal : ISignal
{
    private readonly DetectionConfig _config;
    
    public string Name => "Ubicación Inusual";
    public double Weight => _config.Weights.UnusualLocation;

    public UnusualLocationSignal(DetectionConfig config)
    {
        _config = config;
    }

    public Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        var country = currentEvent.Country ?? "Unknown";
        var city = currentEvent.City ?? "Unknown";
        
        // Si no hay perfil suficiente, no disparar señal (nuevo usuario)
        if (profile.TotalAccessCount < _config.MinimumAccessesForProfile)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "Perfil insuficiente para evaluar ubicación"
            ));
        }

        var isKnownCountry = profile.CommonCountries.Contains(country);
        var isKnownCity = profile.CommonCities.Contains(city);
        
        if (isKnownCountry && isKnownCity)
        {
            // Ubicación completamente habitual
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Ubicación habitual: {country}, {city}"
            ));
        }
        
        if (isKnownCountry && !isKnownCity)
        {
            // País conocido pero ciudad nueva (riesgo moderado)
            return Task.FromResult(new SignalResult(
                Score: 0.5,
                IsTriggered: true,
                Description: $"Ciudad inusual en país conocido: {city}, {country}"
            ));
        }
        
        // País completamente nuevo (riesgo alto)
        return Task.FromResult(new SignalResult(
            Score: 1.0,
            IsTriggered: true,
            Description: $"País no habitual: {country}, {city}. Países habituales: {string.Join(", ", profile.CommonCountries)}"
        ));
    }
}
