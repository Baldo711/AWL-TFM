using AccessWatchLite.Application.Response;
using AccessWatchLite.Domain;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Infrastructure.Response.Actions;

/// <summary>
/// Temporarily disables user account using Microsoft Graph API.
/// NOTE: Requires Microsoft Graph SDK integration. Currently logs the requirement.
/// </summary>
public sealed class BlockUserAction : IResponseAction
{
    private readonly ILogger<BlockUserAction> _logger;

    public string ActionType => "BlockUser";

    public BlockUserAction(ILogger<BlockUserAction> logger)
    {
        _logger = logger;
    }

    public Task<ResponseResult> ExecuteAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(alert.UserId))
            {
                return Task.FromResult(ResponseResult.Failed("UserId is null or empty", "Cannot block user without UserId"));
            }

            _logger.LogWarning(
                "CRITICAL: Block user account requested for {UserId} due to HIGH severity alert {AlertId} (Simulation: {IsSimulation})",
                alert.UserId, alert.Id, alert.IsSimulation);

            // In simulation mode, just log without actual API call
            if (alert.IsSimulation)
            {
                _logger.LogInformation("SIMULATION: Would block user {UserId}", alert.UserId);
                return Task.FromResult(ResponseResult.Successful(
                    $"[SIMULATION] User account {alert.UserId} blocked",
                    $"Alert: {alert.Id}, Severity: {alert.Severity}, RiskScore: {alert.RiskScore}"));
            }

            // NOTE: Microsoft Graph API integration requires:
            // 1. Install NuGet packages: Microsoft.Graph, Microsoft.Graph.Beta (if needed)
            // 2. Configure Azure AD app with User.ReadWrite.All permission
            // 3. Inject GraphServiceClient with appropriate credentials
            // 4. Uncomment Graph code below
            //
            // Example implementation:
            // var updateUser = new User { AccountEnabled = false };
            // await _graphClient.Users[alert.UserId].PatchAsync(updateUser, cancellationToken);
            //
            // For now, log the requirement for manual action

            _logger.LogCritical(
                "USER BLOCK REQUIRED for {UserId}. MANUAL ACTION REQUIRED: Disable account in Azure AD immediately.",
                alert.UserId);

            return Task.FromResult(ResponseResult.Successful(
                $"User block logged for {alert.UserId}",
                $"Alert: {alert.Id}. MANUAL ACTION REQUIRED: Disable account in Azure AD portal or via PowerShell/CLI."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing block user request for {UserId}", alert.UserId);
            return Task.FromResult(ResponseResult.Failed(
                $"Failed to process block user request for {alert.UserId}",
                ex.Message));
        }
    }
}
