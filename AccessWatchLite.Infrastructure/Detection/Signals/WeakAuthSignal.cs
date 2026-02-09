using AccessWatchLite.Application.Detection;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Detection.Signals;

/// <summary>
/// Señal que detecta uso de métodos de autenticación débiles o inusuales
/// </summary>
public class WeakAuthSignal : ISignal
{
    private readonly DetectionConfig _config;
    
    public string Name => "Autenticación Débil";
    public double Weight => _config.Weights.WeakAuth;

    public WeakAuthSignal(DetectionConfig config)
    {
        _config = config;
    }

    public Task<SignalResult> EvaluateAsync(
        AccessEvent currentEvent, 
        UserBehaviorProfile profile, 
        CancellationToken cancellationToken = default)
    {
        var authMethod = currentEvent.AuthMethod;
        
        // Si no hay información de método de autenticación
        if (string.IsNullOrEmpty(authMethod))
        {
            // Verificar Conditional Access como alternativa
            var conditionalAccess = currentEvent.ConditionalAccess ?? "unknown";
            
            if (conditionalAccess.Equals("failure", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new SignalResult(
                    Score: 0.7,
                    IsTriggered: true,
                    Description: "Acceso sin cumplir políticas de Conditional Access"
                ));
            }
            
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: "Método de autenticación no especificado"
            ));
        }
        
        // Si no hay perfil suficiente, solo evaluar si es método débil conocido
        if (profile.TotalAccessCount < _config.MinimumAccessesForProfile)
        {
            if (IsWeakAuthMethod(authMethod))
            {
                return Task.FromResult(new SignalResult(
                    Score: 0.6,
                    IsTriggered: true,
                    Description: $"Método de autenticación potencialmente débil: {authMethod}"
                ));
            }
            
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Método de autenticación: {authMethod}"
            ));
        }

        // Verificar si el método es habitual para este usuario
        var isUsualMethod = profile.UsualAuthMethods.Contains(authMethod);
        
        if (isUsualMethod)
        {
            return Task.FromResult(new SignalResult(
                Score: 0.0,
                IsTriggered: false,
                Description: $"Método de autenticación habitual: {authMethod}"
            ));
        }
        
        // Método inusual para este usuario
        var score = IsWeakAuthMethod(authMethod) ? 0.8 : 0.5;
        
        return Task.FromResult(new SignalResult(
            Score: score,
            IsTriggered: true,
            Description: $"Método de autenticación inusual: {authMethod}. Métodos habituales: {string.Join(", ", profile.UsualAuthMethods)}"
        ));
    }
    
    private static bool IsWeakAuthMethod(string authMethod)
    {
        // Métodos considerados débiles
        var weakMethods = new[] 
        { 
            "password", 
            "passwordless", 
            "sms", 
            "voicecall",
            "oath" // OATH sin segundo factor
        };
        
        return weakMethods.Any(weak => authMethod.Contains(weak, StringComparison.OrdinalIgnoreCase));
    }
}
