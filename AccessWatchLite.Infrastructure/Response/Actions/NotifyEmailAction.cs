using AccessWatchLite.Application.Response;
using AccessWatchLite.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AccessWatchLite.Infrastructure.Response.Actions;

/// <summary>
/// Sends email notification to security team about security incidents.
/// NOTE: Requires SendGrid API integration. Currently logs to Application Insights.
/// </summary>
public sealed class NotifyEmailAction : IResponseAction
{
    private readonly ILogger<NotifyEmailAction> _logger;
    private readonly string _securityTeamEmail;

    public string ActionType => "NotifyEmail";

    public NotifyEmailAction(
        ILogger<NotifyEmailAction> logger,
        string securityTeamEmail = "security-team@company.com")
    {
        _logger = logger;
        _securityTeamEmail = securityTeamEmail;
    }

    public Task<ResponseResult> ExecuteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Email notification requested for alert {AlertId} (Severity: {Severity}, Simulation: {IsSimulation})",
                alert.Id, alert.Severity, alert.IsSimulation);

            // Build email content
            var subject = $"[AccessWatchLite] {alert.Severity} Security Alert - {alert.UserId}";
            var body = BuildEmailBody(alert);

            // In simulation mode, just log without sending
            if (alert.IsSimulation)
            {
                _logger.LogInformation(
                    "SIMULATION: Would send email to {Email}\nSubject: {Subject}\nBody:\n{Body}",
                    _securityTeamEmail, subject, body);
                return Task.FromResult(ResponseResult.Successful(
                    $"[SIMULATION] Email notification prepared",
                    $"To: {_securityTeamEmail}, Subject: {subject}"));
            }

            // NOTE: SendGrid integration requires:
            // 1. Install NuGet package: SendGrid
            // 2. Configure SendGrid API key in Key Vault
            // 3. Uncomment SendGrid code below and inject ISendGridClient
            //
            // For now, log to Application Insights for monitoring
            _logger.LogWarning(
                "EMAIL NOTIFICATION REQUIRED: To: {Email}, Subject: {Subject}\n{Body}",
                _securityTeamEmail, subject, body);

            return Task.FromResult(ResponseResult.Successful(
                $"Email notification logged for {_securityTeamEmail}",
                $"Alert: {alert.Id}. MANUAL ACTION: Configure SendGrid to enable automated emails."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing email notification for alert {AlertId}", alert.Id);
            return Task.FromResult(ResponseResult.Failed(
                $"Failed to prepare email notification",
                ex.Message));
        }
    }

    private static string BuildEmailBody(Alert alert)
    {
        var signals = string.Empty;
        if (!string.IsNullOrEmpty(alert.DetectedSignals))
        {
            try
            {
                var signalsDoc = JsonDocument.Parse(alert.DetectedSignals);
                signals = JsonSerializer.Serialize(signalsDoc, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                signals = alert.DetectedSignals;
            }
        }

        return $@"
=== SECURITY ALERT DETECTED ===

Alert ID: {alert.Id}
User: {alert.UserId}
Severity: {alert.Severity}
Risk Score: {alert.RiskScore:F2}
Status: {alert.Status}
Detected At: {alert.DetectedAt:yyyy-MM-dd HH:mm:ss} UTC

Event Details:
- Event ID: {alert.EventId}
- Location: {alert.City}, {alert.Country}
- IP Address: {alert.IpAddress}
- Device: {alert.DeviceId}

Detected Signals:
{signals}

Action Required:
1. Review the alert in the AccessWatchLite Dashboard
2. Investigate the user's recent activity
3. Contact the user if necessary to verify the activity
4. Mark the alert as Investigating, Resolved, or FalsePositive

Dashboard URL: https://your-app.azurewebsites.net/alerts

---
This is an automated message from AccessWatchLite Security Monitoring System.
";
    }
}

