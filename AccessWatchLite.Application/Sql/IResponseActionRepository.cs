using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

/// <summary>
/// Repository for response_Actions table operations.
/// </summary>
public interface IResponseActionRepository
{
    /// <summary>
    /// Inserts a new response action record.
    /// </summary>
    Task InsertAsync(ResponseAction action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all response actions for a specific alert.
    /// </summary>
    Task<List<ResponseAction>> GetByAlertIdAsync(Guid alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending response actions (status = 'Pending').
    /// </summary>
    Task<List<ResponseAction>> GetPendingAsync(bool isSimulation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status and result of a response action.
    /// </summary>
    Task UpdateStatusAsync(
        Guid id,
        string status,
        string? result,
        string? errorMessage,
        CancellationToken cancellationToken = default);
}
