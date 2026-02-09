using System.Text.Json;
using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Functions.Functions;

/// <summary>
/// Función para detectar anomalías en eventos de acceso.
/// Analiza patrones sospechosos usando el motor de detección basado en señales de riesgo.
/// </summary>
public sealed class DetectionFunction
{
    private readonly IRiskDetectionEngine _detectionEngine;
    private readonly IAccessEventRepository _eventRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<DetectionFunction> _logger;

    public DetectionFunction(
        IRiskDetectionEngine detectionEngine,
        IAccessEventRepository eventRepository,
        IAlertRepository alertRepository,
        ILogger<DetectionFunction> logger)
    {
        _detectionEngine = detectionEngine;
        _eventRepository = eventRepository;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    /// <summary>
    /// Se ejecuta cada 1 minuto para analizar eventos no procesados
    /// Cron: "0 * * * * *" = cada minuto
    /// NOTA: Solo procesa eventos REALES. Los eventos SIM se analizan bajo demanda.
    /// </summary>
    [Function(nameof(DetectionFunction))]
    public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timer, FunctionContext context)
    {
        _logger.LogInformation("Detection function triggered at: {Time}", DateTime.UtcNow);

        try
        {
            // IMPORTANTE: Solo analizar eventos REALES automáticamente
            // Los eventos de simulación se procesan bajo demanda desde AnalyzeSimEventsFunction
            _logger.LogInformation("Analyzing REAL events only (SIM events are processed on-demand)");
            await AnalyzeEventsAsync(isSimulation: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in detection function");
            throw;
        }

        _logger.LogInformation("Detection function completed at: {Time}", DateTime.UtcNow);
    }

    private async Task AnalyzeEventsAsync(bool isSimulation)
    {
        var mode = isSimulation ? "SIMULATION" : "REAL";
        _logger.LogInformation("Analyzing {Mode} events...", mode);

        // Obtener eventos no analizados (batch de 100)
        var events = await _eventRepository.GetUnanalyzedEventsAsync(isSimulation, batchSize: 100);

        if (events.Count == 0)
        {
            _logger.LogInformation("No unanalyzed {Mode} events found", mode);
            return;
        }

        _logger.LogInformation("Found {Count} unanalyzed {Mode} events", events.Count, mode);

        var alertsCreated = 0;

        foreach (var accessEvent in events)
        {
            try
            {
                // Analizar evento con el motor de detección
                var alert = await _detectionEngine.AnalyzeEventAsync(accessEvent, isSimulation);

                // Si el motor generó una alerta, insertarla
                if (alert != null)
                {
                    // Ajustar DetectedAt a zona horaria de España
                    alert.DetectedAt = GetSpainLocalTime();
                    
                    await _alertRepository.InsertAsync(alert);
                    alertsCreated++;

                    _logger.LogWarning(
                        "ALERT created: {Severity} risk for user {User} - Score: {Score:F2}",
                        alert.Severity, accessEvent.UserPrincipalName, alert.RiskScore);
                }

                // Marcar evento como analizado
                await _eventRepository.MarkAsAnalyzedAsync(accessEvent.Id, isSimulation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing event {EventId}", accessEvent.EventId);
            }
        }

        _logger.LogInformation(
            "Analysis complete: {Analyzed} {Mode} events processed, {Alerts} alerts created",
            events.Count, mode, alertsCreated);
    }
    
    private DateTime GetSpainLocalTime()
    {
        TimeZoneInfo spainTimeZone;
        
        try
        {
            // Intentar con formato Windows (desarrollo local)
            spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // En Linux/Azure, usar formato IANA
                spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: UTC+1
                return DateTime.UtcNow.AddHours(1);
            }
        }
        
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
    }
}
