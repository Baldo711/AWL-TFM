using System.Text.Json;
using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Infrastructure.Detection;

/// <summary>
/// Motor de detección basado en combinación de señales de riesgo ponderadas
/// Implementa los principios del TFM: análisis contextual, combinación de señales,
/// interpretabilidad, ajustabilidad y eficiencia
/// </summary>
public sealed class RiskDetectionEngine : IRiskDetectionEngine
{
    private readonly IUserBehaviorProfileRepository _profileRepository;
    private readonly IEnumerable<ISignal> _signals;
    private readonly DetectionConfig _config;
    private readonly ILogger<RiskDetectionEngine> _logger;

    public RiskDetectionEngine(
        IUserBehaviorProfileRepository profileRepository,
        IEnumerable<ISignal> signals,
        DetectionConfig config,
        ILogger<RiskDetectionEngine> logger)
    {
        _profileRepository = profileRepository;
        _signals = signals;
        _config = config;
        _logger = logger;
    }

    public async Task<Alert?> AnalyzeEventAsync(
        AccessEvent accessEvent, 
        bool isSimulation, 
        CancellationToken cancellationToken = default)
    {
        // Solo analizamos eventos REAL (simulador no aplica)
        if (isSimulation)
        {
            _logger.LogDebug("Skipping simulation event {EventId}", accessEvent.EventId);
            return null;
        }

        // Validar que UserId no sea null
        if (string.IsNullOrEmpty(accessEvent.UserId))
        {
            _logger.LogWarning("Event {EventId} has no UserId, skipping analysis", accessEvent.EventId);
            return null;
        }

        try
        {
            // 1. Construir perfil de comportamiento del usuario (análisis contextual)
            var profile = await GetOrBuildUserProfileAsync(accessEvent.UserId, isSimulation, cancellationToken);
            
            // 2. Evaluar todas las señales de riesgo (combinación de señales)
            var signalResults = new List<SignalResult>();
            double totalWeightedScore = 0.0;
            
            foreach (var signal in _signals)
            {
                try
                {
                    var result = await signal.EvaluateAsync(accessEvent, profile, cancellationToken);
                    signalResults.Add(result);
                    
                    if (result.IsTriggered)
                    {
                        // Contribución ponderada al score total
                        totalWeightedScore += result.Score * signal.Weight;
                        
                        _logger.LogDebug(
                            "Signal '{SignalName}' triggered for event {EventId}: Score={Score:F2}, Weight={Weight:F2}",
                            signal.Name, accessEvent.EventId, result.Score, signal.Weight);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error evaluating signal {SignalName} for event {EventId}", 
                        signal.Name, accessEvent.EventId);
                }
            }
            
            // 3. Normalizar score agregado a escala 0-100
            var riskScore = Math.Min(totalWeightedScore * 100, 100);
            
            _logger.LogInformation(
                "Event {EventId} analyzed: RiskScore={RiskScore:F2}, TriggeredSignals={Count}",
                accessEvent.EventId, riskScore, signalResults.Count(s => s.IsTriggered));
            
            // 4. Si no supera el umbral mínimo, no generar alerta
            if (riskScore < _config.MinimumAlertThreshold)
            {
                return null;
            }
            
            // 5. Determinar severidad según umbrales (ajustabilidad)
            var severity = DetermineSeverity(riskScore);
            
            // 6. Crear alerta con señales detectadas (interpretabilidad)
            var triggeredSignals = signalResults.Where(s => s.IsTriggered).ToList();
            var alert = CreateAlert(accessEvent, riskScore, severity, triggeredSignals);
            
            _logger.LogWarning(
                "ALERT CREATED: Event={EventId}, User={User}, RiskScore={Score:F2}, Severity={Severity}, Signals={SignalCount}",
                accessEvent.EventId, accessEvent.UserPrincipalName, riskScore, severity, triggeredSignals.Count);
            
            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing event {EventId}", accessEvent.EventId);
            return null;
        }
    }

    /// <summary>
    /// Obtiene o construye el perfil de comportamiento del usuario
    /// </summary>
    private async Task<UserBehaviorProfile> GetOrBuildUserProfileAsync(
        string userId,
        bool isSimulation,
        CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-_config.ProfileLookbackDays);
        return await _profileRepository.BuildProfileAsync(userId, isSimulation, since, cancellationToken);
    }

    /// <summary>
    /// Determina la severidad de la alerta según el score de riesgo
    /// </summary>
    private string DetermineSeverity(double riskScore)
    {
        if (riskScore >= _config.HighSeverityThreshold)
            return "High";
        
        if (riskScore >= _config.MediumSeverityThreshold)
            return "Medium";
        
        return "Low";
    }

    /// <summary>
    /// Crea una alerta a partir del análisis de riesgo
    /// </summary>
    private Alert CreateAlert(
        AccessEvent accessEvent, 
        double riskScore, 
        string severity,
        List<SignalResult> triggeredSignals)
    {
        // Construir título descriptivo
        var title = severity switch
        {
            "High" => $"Acceso de alto riesgo detectado - {accessEvent.UserPrincipalName}",
            "Medium" => $"Acceso sospechoso detectado - {accessEvent.UserPrincipalName}",
            _ => $"Acceso con anomalías detectado - {accessEvent.UserPrincipalName}"
        };

        // Construir descripción con las señales principales
        var topSignals = triggeredSignals
            .OrderByDescending(s => s.Score)
            .Take(3)
            .Select(s => s.Description);
        
        var description = $"Se detectaron {triggeredSignals.Count} señal(es) de riesgo: " +
                         string.Join("; ", topSignals);

        // Serializar todas las señales disparadas para interpretabilidad
        var detectedSignalsJson = JsonSerializer.Serialize(
            triggeredSignals.Select(s => new
            {
                Signal = s.Description,
                Score = Math.Round(s.Score, 2)
            }),
            new JsonSerializerOptions { WriteIndented = false });

        return new Alert
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Severity = severity,
            Status = "New",
            RiskScore = (decimal)riskScore,
            UserId = accessEvent.UserId,
            UserPrincipalName = accessEvent.UserPrincipalName,
            EventTimestamp = accessEvent.TimestampUtc,
            IpAddress = accessEvent.IpAddress,
            Country = accessEvent.Country,
            City = accessEvent.City,
            DeviceId = accessEvent.DeviceId,
            DetectedSignals = detectedSignalsJson,
            DetectedAt = DateTime.UtcNow, // Se ajustará a España en la Function
            IsSimulation = false // Solo eventos REAL
        };
    }
}
