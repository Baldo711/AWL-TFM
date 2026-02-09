using AccessWatchLite.Application.Response;
using AccessWatchLite.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AccessWatchLite.Infrastructure.Response.Actions;

/// <summary>
/// Logs security incident to Application Insights and local logs.
/// Creates audit trail for all security events.
/// </summary>
public sealed class LogIncidentAction : IResponseAction
{
    private readonly ILogger<LogIncidentAction> _logger;

    public string ActionType => "LogIncident";

    public LogIncidentAction(ILogger<LogIncidentAction> logger)
    {
        _logger = logger;
    }

    public Task<ResponseResult> ExecuteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            // Log comprehensive incident details
            var incidentDetails = new
            {
                AlertId = alert.Id,
                EventId = alert.EventId,
                UserId = alert.UserId,
                Severity = alert.Severity,
                RiskScore = alert.RiskScore,
                Status = alert.Status,
                DetectedAt = alert.DetectedAt,
                City = alert.City,
                Country = alert.Country,
                IpAddress = alert.IpAddress,
                DeviceId = alert.DeviceId,
                DetectedSignals = alert.DetectedSignals,
                IsSimulation = alert.IsSimulation
            };

            var incidentJson = JsonSerializer.Serialize(incidentDetails, new JsonSerializerOptions { WriteIndented = true });

            // Log based on severity
            switch (alert.Severity)
            {
                case "HIGH":
                    _logger.LogCritical(
                        "SECURITY INCIDENT [HIGH]: Alert {AlertId} for user {UserId}. RiskScore: {RiskScore:F2}. Details: {Details}",
                        alert.Id, alert.UserId, alert.RiskScore, incidentJson);
                    break;

                case "MEDIUM":
                    _logger.LogWarning(
                        "SECURITY INCIDENT [MEDIUM]: Alert {AlertId} for user {UserId}. RiskScore: {RiskScore:F2}. Details: {Details}",
                        alert.Id, alert.UserId, alert.RiskScore, incidentJson);
                    break;

                case "LOW":
                    _logger.LogInformation(
                        "SECURITY INCIDENT [LOW]: Alert {AlertId} for user {UserId}. RiskScore: {RiskScore:F2}. Details: {Details}",
                        alert.Id, alert.UserId, alert.RiskScore, incidentJson);
                    break;

                default:
                    _logger.LogInformation(
                        "SECURITY INCIDENT: Alert {AlertId} for user {UserId}. RiskScore: {RiskScore:F2}. Details: {Details}",
                        alert.Id, alert.UserId, alert.RiskScore, incidentJson);
                    break;
            }

            // Add simulation tag if applicable
            if (alert.IsSimulation)
            {
                _logger.LogInformation("SIMULATION incident logged for alert {AlertId}", alert.Id);
            }

            return Task.FromResult(ResponseResult.Successful(
                $"Incident logged for alert {alert.Id}",
                $"Severity: {alert.Severity}, RiskScore: {alert.RiskScore:F2}, Logged to Application Insights"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging incident for alert {AlertId}", alert.Id);
            return Task.FromResult(ResponseResult.Failed(
                $"Failed to log incident for alert {alert.Id}",
                ex.Message));
        }
    }
}
