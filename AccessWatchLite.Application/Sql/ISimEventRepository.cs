using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface ISimEventRepository
{
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task InsertAsync(AccessEvent accessEvent, CancellationToken cancellationToken = default);
}
