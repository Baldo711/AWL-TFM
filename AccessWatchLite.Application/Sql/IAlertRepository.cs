using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetRecentAlertsAsync(bool isSimulation, int count = 50, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetPendingAlertsAsync(bool isSimulation, CancellationToken cancellationToken = default);
    Task InsertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, string status, string? resolution = null, CancellationToken cancellationToken = default);
}
