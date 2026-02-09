using System.Data.Common;

namespace AccessWatchLite.Application.Sql;

public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
