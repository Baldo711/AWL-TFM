using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Services;

/// <summary>
/// Service for orchestrating automated response actions on security alerts.
/// </summary>
public interface IResponseService
{
    /// <summary>
    /// Executes specified response actions for a given alert.
    /// </summary>
    /// <param name="alert">Alert to respond to</param>
    /// <param name="actionTypes">List of action types to execute (RevokeSession, BlockUser, RequireMfa, NotifyEmail, LogIncident)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of successfully executed actions</returns>
    Task<int> ExecuteActionsForAlertAsync(
        Alert alert,
        List<string> actionTypes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending response actions that have not been executed yet.
    /// </summary>
    /// <param name="isSimulation">Filter by simulation mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending response actions</returns>
    Task<List<ResponseAction>> GetPendingActionsAsync(
        bool isSimulation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all response actions for a specific alert.
    /// </summary>
    /// <param name="alertId">Alert ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of response actions</returns>
    Task<List<ResponseAction>> GetActionsForAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default);
}
