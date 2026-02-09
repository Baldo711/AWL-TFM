using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface ISimMetadataRepository
{
    Task<SimMetadata?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task UpdateFromEventsAsync(string updatedBy, CancellationToken cancellationToken = default);
}
