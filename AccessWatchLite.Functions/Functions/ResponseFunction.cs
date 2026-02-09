using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Functions.Functions;

/// <summary>
/// Función para responder automáticamente a incidentes de seguridad detectados.
/// Ejecuta acciones según la severidad: bloquear usuarios, revocar sesiones, notificaciones.
/// Runs every 15 minutes to process new HIGH severity alerts.
/// </summary>
public sealed class ResponseFunction
{
    private readonly ILogger<ResponseFunction> _logger;
    private readonly IAlertRepository _alertRepository;
    private readonly IResponseService _responseService;

    public ResponseFunction(
        ILogger<ResponseFunction> logger,
        IAlertRepository alertRepository,
        IResponseService responseService)
    {
        _logger = logger;
        _alertRepository = alertRepository;
        _responseService = responseService;
    }

    /// <summary>
    /// Runs every 15 minutes to process new alerts and execute automated response actions.
    /// </summary>
    [Function(nameof(ResponseFunction))]
    public async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo timer, FunctionContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Response function triggered at: {Time}", startTime);

        try
        {
            // Process REAL alerts (IsSimulation = false)
            await ProcessAlertsAsync(isSimulation: false, context.CancellationToken);

            // Optionally process SIMULATION alerts separately
            // await ProcessAlertsAsync(isSimulation: true, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Response function execution");
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        _logger.LogInformation(
            "Response function completed at: {Time}. Duration: {Duration}ms",
            endTime, duration.TotalMilliseconds);
    }

    private async Task ProcessAlertsAsync(bool isSimulation, CancellationToken cancellationToken)
    {
        // Get pending (New status) HIGH severity alerts that need response
        var allAlerts = await _alertRepository.GetPendingAlertsAsync(isSimulation, cancellationToken);
        var highAlerts = allAlerts.Where(a => a.Severity == "HIGH").ToList();

        if (highAlerts.Count == 0)
        {
            _logger.LogInformation(
                "No new HIGH severity alerts to process (IsSimulation: {IsSimulation})",
                isSimulation);
            return;
        }

        _logger.LogInformation(
            "Processing {Count} HIGH severity alerts (IsSimulation: {IsSimulation})",
            highAlerts.Count, isSimulation);

        foreach (var alert in highAlerts)
        {
            try
            {
                // Determine actions based on risk score and severity
                var actions = DetermineActions(alert);

                _logger.LogInformation(
                    "Executing {ActionCount} actions for alert {AlertId} (RiskScore: {RiskScore}, User: {UserId})",
                    actions.Count, alert.Id, alert.RiskScore, alert.UserId);

                // Execute all actions
                var successCount = await _responseService.ExecuteActionsForAlertAsync(
                    alert,
                    actions,
                    cancellationToken);

                // Update alert status to Investigating
                await _alertRepository.UpdateStatusAsync(alert.Id, "Investigating", null, cancellationToken);

                _logger.LogInformation(
                    "Alert {AlertId} processed: {SuccessCount}/{TotalCount} actions executed successfully. Status updated to Investigating.",
                    alert.Id, successCount, actions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing alert {AlertId} for user {UserId}",
                    alert.Id, alert.UserId);
            }
        }
    }

    /// <summary>
    /// Determines which response actions to execute based on alert properties.
    /// </summary>
    private List<string> DetermineActions(Domain.Alert alert)
    {
        var actions = new List<string>();

        // Always log all incidents
        actions.Add("LogIncident");

        // RiskScore >= 80: Critical threat - Block user and revoke sessions
        if (alert.RiskScore >= 80)
        {
            actions.Add("BlockUser");
            actions.Add("RevokeSession");
            actions.Add("NotifyEmail");
            _logger.LogWarning(
                "CRITICAL alert {AlertId} (RiskScore: {RiskScore}) - Will block user and revoke sessions",
                alert.Id, alert.RiskScore);
        }
        // RiskScore >= 70: High threat - Revoke sessions only
        else if (alert.RiskScore >= 70)
        {
            actions.Add("RevokeSession");
            actions.Add("NotifyEmail");
            _logger.LogWarning(
                "HIGH alert {AlertId} (RiskScore: {RiskScore}) - Will revoke sessions",
                alert.Id, alert.RiskScore);
        }
        // RiskScore >= 60: Moderate threat - Notify only
        else if (alert.RiskScore >= 60)
        {
            actions.Add("NotifyEmail");
            _logger.LogInformation(
                "MODERATE alert {AlertId} (RiskScore: {RiskScore}) - Will notify security team",
                alert.Id, alert.RiskScore);
        }

        return actions;
    }
}
