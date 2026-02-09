using System.Net;
using System.Text.Json;
using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Functions.Functions;

/// <summary>
/// HTTP trigger para análisis bajo demanda de eventos simulados
/// </summary>
public sealed class AnalyzeSimEventsFunction
{
    private readonly IAccessEventRepository _eventRepository;
    private readonly ISimMetadataRepository _metadataRepository;
    private readonly IRiskDetectionEngine _detectionEngine;
    private readonly IAlertRepository _alertRepository;
    private readonly IAnalysisProgressService _progressService;
    private readonly ILogger<AnalyzeSimEventsFunction> _logger;

    public AnalyzeSimEventsFunction(
        IAccessEventRepository eventRepository,
        ISimMetadataRepository metadataRepository,
        IRiskDetectionEngine detectionEngine,
        IAlertRepository alertRepository,
        IAnalysisProgressService progressService,
        ILogger<AnalyzeSimEventsFunction> logger)
    {
        _eventRepository = eventRepository;
        _metadataRepository = metadataRepository;
        _detectionEngine = detectionEngine;
        _alertRepository = alertRepository;
        _progressService = progressService;
        _logger = logger;
    }

    [Function(nameof(AnalyzeSimEventsFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        FunctionContext context)
    {
        _logger.LogInformation("Manual analysis of simulation events triggered");

        try
        {
            // Leer parámetros del body
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<AnalysisRequest>(body);

            if (request == null)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
            }

            _logger.LogInformation("Analyzing events from {StartDate} to {EndDate}", 
                request.StartDate?.ToString("yyyy-MM-dd") ?? "beginning", 
                request.EndDate?.ToString("yyyy-MM-dd") ?? "end");

            var result = await AnalyzeEventsAsync(request.StartDate, request.EndDate, request.BatchSize ?? 1000);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual analysis");
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    private async Task<AnalysisResult> AnalyzeEventsAsync(DateTime? startDate, DateTime? endDate, int batchSize)
    {
        var startTime = DateTime.UtcNow;
        var totalProcessed = 0;
        var totalAlerts = 0;
        var alertsBySeverity = new Dictionary<string, int>
        {
            ["High"] = 0,
            ["Medium"] = 0,
            ["Low"] = 0
        };

        // Obtener eventos no analizados en el rango de fechas
        var allEvents = await _eventRepository.GetUnanalyzedEventsAsync(isSimulation: true, batchSize);
        
        // Filtrar por fecha si se especifica
        if (startDate.HasValue)
        {
            allEvents = allEvents.Where(e => e.TimestampUtc >= startDate.Value).ToList();
        }
        if (endDate.HasValue)
        {
            allEvents = allEvents.Where(e => e.TimestampUtc <= endDate.Value.AddDays(1).AddSeconds(-1)).ToList();
        }


        _logger.LogInformation("Found {Count} unanalyzed events to process", allEvents.Count);

        // Iniciar tracking de progreso
        _progressService.StartAnalysis(allEvents.Count);

        foreach (var evt in allEvents)
        {
            try
            {
                var alert = await _detectionEngine.AnalyzeEventAsync(evt, isSimulation: true);
                
                totalProcessed++;

                if (alert != null)
                {
                    // TODO: Motor de detección para simulador - por implementar
                    // Por ahora solo marcamos como analizado
                    await _alertRepository.InsertAsync(alert);
                    
                    totalAlerts++;
                    alertsBySeverity[alert.Severity]++;
                }

                await _eventRepository.MarkAsAnalyzedAsync(evt.Id, isSimulation: true);

                // Actualizar progreso cada evento (o cada N eventos si prefieres)
                if (totalProcessed % 10 == 0 || totalProcessed == allEvents.Count)
                {
                    _progressService.UpdateProgress(
                        totalProcessed,
                        totalAlerts,
                        evt.TimestampUtc,
                        evt.UserPrincipalName ?? "Unknown"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing event {EventId}", evt.Id);
            }
        }

        // Completar análisis
        _progressService.CompleteAnalysis();

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Analysis completed: {Processed} events processed, {Alerts} alerts created in {Duration}s",
            totalProcessed, totalAlerts, duration.TotalSeconds);

        // Actualizar metadatos después del análisis
        if (totalProcessed > 0)
        {
            await _metadataRepository.UpdateFromEventsAsync("AnalyzeSimEventsFunction");
            _logger.LogInformation("Metadata updated after analysis");
        }

        return new AnalysisResult
        {
            Success = true,
            EventsProcessed = totalProcessed,
            AlertsCreated = totalAlerts,
            AlertsBySeverity = alertsBySeverity,
            DurationSeconds = duration.TotalSeconds,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    private Alert CreateAlert(AccessEvent evt, RiskAnalysisResult analysis, bool isSimulation)
    {
        var detectedSignalsJson = analysis.DetectedSignals != null && analysis.DetectedSignals.Any()
            ? JsonSerializer.Serialize(analysis.DetectedSignals)
            : null;

        return new Alert
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            UserId = evt.UserId,
            UserPrincipalName = evt.UserPrincipalName,
            IsSimulation = isSimulation,
            Severity = analysis.Severity,
            RiskScore = analysis.TotalRiskScore,
            Status = "New",
            Title = $"Acceso anómalo detectado - Riesgo {analysis.Severity}",
            Description = $"Se detectaron {analysis.DetectedSignals?.Count ?? 0} señales de riesgo en este acceso.",
            DetectedSignals = detectedSignalsJson,
            EventTimestamp = evt.TimestampUtc,
            IpAddress = evt.IpAddress,
            Country = evt.Country,
            City = evt.City,
            DeviceId = evt.DeviceId,
            DetectedAt = GetSpainLocalTime() // Hora local de España
        };
    }

    private static DateTime GetSpainLocalTime()
    {
        try
        {
            // Intentar con formato Windows (desarrollo local)
            var spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // En Linux/Azure, usar formato IANA
            var spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
        }
    }

    private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    // DTOs
    private class AnalysisRequest
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? BatchSize { get; set; }
    }

    private class AnalysisResult
    {
        public bool Success { get; set; }
        public int EventsProcessed { get; set; }
        public int AlertsCreated { get; set; }
        public Dictionary<string, int> AlertsBySeverity { get; set; } = new();
        public double DurationSeconds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
