using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IAccessEventRepository _accessEventRepository;
    private readonly IAlertService _alertService;

    public DashboardService(
        IAccessEventRepository accessEventRepository,
        IAlertService alertService)
    {
        _accessEventRepository = accessEventRepository;
        _alertService = alertService;
    }

    public async Task<List<AccessEvent>> GetRecentAccessesAsync(bool isSimulation, int count = 5, CancellationToken cancellationToken = default)
    {
        return await _accessEventRepository.GetRecentAsync(isSimulation, count, cancellationToken);
    }

    public async Task<List<AccessEvent>> GetAccessEventsAsync(bool isSimulation, int count = 100, CancellationToken cancellationToken = default)
    {
        return await _accessEventRepository.GetEventsAsync(isSimulation, count, cancellationToken);
    }

    public async Task<AlertsSummary> GetAlertsSummaryAsync(bool isSimulation, CancellationToken cancellationToken = default)
    {
        var stats = await _alertService.GetAlertStatisticsAsync(isSimulation, cancellationToken);
        
        var highCount = stats.TryGetValue("High", out var high) ? high : 0;
        var mediumCount = stats.TryGetValue("Medium", out var medium) ? medium : 0;
        var lowCount = stats.TryGetValue("Low", out var low) ? low : 0;
        var totalCount = stats.TryGetValue("Total", out var total) ? total : 0;

        return new AlertsSummary(highCount, mediumCount, lowCount, totalCount);
    }
}
