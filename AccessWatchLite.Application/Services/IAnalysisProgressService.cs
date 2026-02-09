namespace AccessWatchLite.Application.Services;

/// <summary>
/// Servicio para trackear el progreso de análisis en tiempo real
/// </summary>
public interface IAnalysisProgressService
{
    void StartAnalysis(int totalEvents);
    void UpdateProgress(int processedEvents, int alertsCreated, DateTime lastProcessedDate, string lastProcessedUser);
    void CompleteAnalysis();
    AnalysisProgress GetCurrentProgress();
}
