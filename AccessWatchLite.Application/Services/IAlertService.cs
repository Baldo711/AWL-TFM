using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Services;

public interface IAlertService
{
    Task<List<Alert>> GetRecentAlertsAsync(bool? isSimulation = null, int count = 50, CancellationToken cancellationToken = default);
    Task<List<Alert>> GetPendingAlertsAsync(bool? isSimulation = null, CancellationToken cancellationToken = default);
    Task<Alert?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAlertStatusAsync(Guid id, string status, string? resolution = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetAlertStatisticsAsync(bool? isSimulation = null, CancellationToken cancellationToken = default);
}
