namespace AccessWatchLite.Application.Response;

/// <summary>
/// Interface for response actions that can be executed on security alerts.
/// Based on TFM security response framework.
/// </summary>
public interface IResponseAction
{
    /// <summary>
    /// Unique identifier for the action type (RevokeSession, BlockUser, RequireMfa, NotifyEmail, LogIncident)
    /// </summary>
    string ActionType { get; }

    /// <summary>
    /// Executes the response action for a given alert.
    /// </summary>
    /// <param name="alert">Alert entity that triggered the response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the action execution</returns>
    Task<ResponseResult> ExecuteAsync(Domain.Alert alert, CancellationToken cancellationToken = default);
}
