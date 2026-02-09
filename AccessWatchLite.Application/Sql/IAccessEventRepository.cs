using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface IAccessEventRepository
{
    Task<List<AccessEvent>> GetUnanalyzedEventsAsync(bool isSimulation, int batchSize = 100, CancellationToken cancellationToken = default);
    Task<List<AccessEvent>> GetRecentEventsByUserAsync(string userId, bool isSimulation, DateTime since, CancellationToken cancellationToken = default);
    Task<List<AccessEvent>> GetRecentAsync(bool isSimulation, int count = 5, CancellationToken cancellationToken = default);
    Task<List<AccessEvent>> GetEventsAsync(bool isSimulation, int count = 100, CancellationToken cancellationToken = default);
    Task MarkAsAnalyzedAsync(Guid eventId, bool isSimulation, CancellationToken cancellationToken = default);
    Task<int> CountFailedAttemptsAsync(string userId, bool isSimulation, DateTime since, CancellationToken cancellationToken = default);
    Task InsertAsync(AccessEvent accessEvent, bool isSimulation, CancellationToken cancellationToken = default);
}
