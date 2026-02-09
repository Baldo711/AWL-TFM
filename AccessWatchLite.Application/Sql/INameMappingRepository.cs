using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface INameMappingRepository
{
    Task<NameMapping?> GetByOriginalHashAsync(string originalHash, CancellationToken cancellationToken = default);
    Task InsertAsync(NameMapping mapping, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
