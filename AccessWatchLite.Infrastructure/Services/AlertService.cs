using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Services;

public sealed class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;

    public AlertService(IAlertRepository alertRepository)
    {
        _alertRepository = alertRepository;
    }

    public async Task<List<Alert>> GetRecentAlertsAsync(bool? isSimulation = null, int count = 50, CancellationToken cancellationToken = default)
    {
        if (isSimulation.HasValue)
        {
            return await _alertRepository.GetRecentAlertsAsync(isSimulation.Value, count, cancellationToken);
        }

        // Si no se especifica modo, obtener ambos
        var simAlerts = await _alertRepository.GetRecentAlertsAsync(true, count / 2, cancellationToken);
        var realAlerts = await _alertRepository.GetRecentAlertsAsync(false, count / 2, cancellationToken);
        
        return simAlerts.Concat(realAlerts)
            .OrderByDescending(a => a.DetectedAt)
            .Take(count)
            .ToList();
    }

    public async Task<List<Alert>> GetPendingAlertsAsync(bool? isSimulation = null, CancellationToken cancellationToken = default)
    {
        if (isSimulation.HasValue)
        {
            return await _alertRepository.GetPendingAlertsAsync(isSimulation.Value, cancellationToken);
        }

        // Si no se especifica modo, obtener ambos
        var simAlerts = await _alertRepository.GetPendingAlertsAsync(true, cancellationToken);
        var realAlerts = await _alertRepository.GetPendingAlertsAsync(false, cancellationToken);
        
        return simAlerts.Concat(realAlerts)
            .OrderByDescending(a => a.RiskScore)
            .ThenBy(a => a.DetectedAt)
            .ToList();
    }

    public async Task<Alert?> GetAlertByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _alertRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task UpdateAlertStatusAsync(Guid id, string status, string? resolution = null, CancellationToken cancellationToken = default)
    {
        await _alertRepository.UpdateStatusAsync(id, status, resolution, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetAlertStatisticsAsync(bool? isSimulation = null, CancellationToken cancellationToken = default)
    {
        var alerts = await GetRecentAlertsAsync(isSimulation, 1000, cancellationToken);
        
        return new Dictionary<string, int>
        {
            ["Total"] = alerts.Count,
            ["High"] = alerts.Count(a => a.Severity == "High"),
            ["Medium"] = alerts.Count(a => a.Severity == "Medium"),
            ["Low"] = alerts.Count(a => a.Severity == "Low"),
            ["New"] = alerts.Count(a => a.Status == "New"),
            ["Investigating"] = alerts.Count(a => a.Status == "Investigating"),
            ["Resolved"] = alerts.Count(a => a.Status == "Resolved"),
            ["FalsePositive"] = alerts.Count(a => a.Status == "FalsePositive")
        };
    }
}
