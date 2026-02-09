using System.Data.Common;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class NameMappingRepository : INameMappingRepository
{
    private const string GetByHashSql = @"
SELECT Id, OriginalHash, PseudonymFirstName, PseudonymLastName, PseudonymFullName, PseudonymEmail, CreatedAt
FROM dbo.name_Mappings
WHERE OriginalHash = @OriginalHash;
";

    private const string InsertSql = @"
INSERT INTO dbo.name_Mappings
(Id, OriginalHash, PseudonymFirstName, PseudonymLastName, PseudonymFullName, PseudonymEmail, CreatedAt)
VALUES
(@Id, @OriginalHash, @PseudonymFirstName, @PseudonymLastName, @PseudonymFullName, @PseudonymEmail, @CreatedAt);
";

    private const string GetCountSql = "SELECT COUNT(*) FROM dbo.name_Mappings;";

    private readonly ISqlConnectionFactory _connectionFactory;

    public NameMappingRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<NameMapping?> GetByOriginalHashAsync(string originalHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetByHashSql;
        AddParameter(command, "@OriginalHash", originalHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapNameMapping(reader);
        }

        return null;
    }

    public async Task InsertAsync(NameMapping mapping, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;

        AddParameter(command, "@Id", mapping.Id);
        AddParameter(command, "@OriginalHash", mapping.OriginalHash);
        AddParameter(command, "@PseudonymFirstName", mapping.PseudonymFirstName);
        AddParameter(command, "@PseudonymLastName", mapping.PseudonymLastName);
        AddParameter(command, "@PseudonymFullName", mapping.PseudonymFullName);
        AddParameter(command, "@PseudonymEmail", mapping.PseudonymEmail);
        AddParameter(command, "@CreatedAt", mapping.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetCountSql;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static NameMapping MapNameMapping(DbDataReader reader)
    {
        return new NameMapping
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            OriginalHash = reader.GetString(reader.GetOrdinal("OriginalHash")),
            PseudonymFirstName = reader.GetString(reader.GetOrdinal("PseudonymFirstName")),
            PseudonymLastName = reader.GetString(reader.GetOrdinal("PseudonymLastName")),
            PseudonymFullName = reader.GetString(reader.GetOrdinal("PseudonymFullName")),
            PseudonymEmail = reader.GetString(reader.GetOrdinal("PseudonymEmail")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
