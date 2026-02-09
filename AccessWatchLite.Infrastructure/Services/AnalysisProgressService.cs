using AccessWatchLite.Application.Services;

namespace AccessWatchLite.Infrastructure.Services;

/// <summary>
/// Servicio singleton para trackear progreso de análisis (compartido entre Functions y UI)
/// </summary>
public sealed class AnalysisProgressService : IAnalysisProgressService
{
    private readonly object _lock = new();
    private AnalysisProgress _currentProgress = new();

    public void StartAnalysis(int totalEvents)
    {
        lock (_lock)
        {
            _currentProgress = new AnalysisProgress
            {
                IsRunning = true,
                TotalEvents = totalEvents,
                ProcessedEvents = 0,
                AlertsCreated = 0
            };
        }
    }

    public void UpdateProgress(int processedEvents, int alertsCreated, DateTime lastProcessedDate, string lastProcessedUser)
    {
        lock (_lock)
        {
            _currentProgress.ProcessedEvents = processedEvents;
            _currentProgress.AlertsCreated = alertsCreated;
            _currentProgress.LastProcessedDate = lastProcessedDate;
            _currentProgress.LastProcessedUser = lastProcessedUser;
        }
    }

    public void CompleteAnalysis()
    {
        lock (_lock)
        {
            _currentProgress.IsRunning = false;
        }
    }

    public AnalysisProgress GetCurrentProgress()
    {
        lock (_lock)
        {
            // Retornar copia para evitar modificaciones externas
            return new AnalysisProgress
            {
                IsRunning = _currentProgress.IsRunning,
                TotalEvents = _currentProgress.TotalEvents,
                ProcessedEvents = _currentProgress.ProcessedEvents,
                AlertsCreated = _currentProgress.AlertsCreated,
                LastProcessedDate = _currentProgress.LastProcessedDate,
                LastProcessedUser = _currentProgress.LastProcessedUser
            };
        }
    }
}
