using System.Data.Common;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class SimEventRepository : ISimEventRepository
{
    private const string ClearSql = "DELETE FROM dbo.sim_Events;";
    private const string InsertSql = @"
INSERT INTO dbo.sim_Events
(
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    TimestampUtc,
    IpAddress,
    Country,
    City,
    DeviceId,
    DeviceName,
    ClientApp,
    ClientResource,
    AuthMethod,
    Status,
    ConditionalAccess,
    Error,
    Result,
    RiskLevel,
    RiskEventTypesJson,
    RawJson,
    IsIgnored,
    CreatedAt
)
VALUES
(
    @Id,
    @EventId,
    @UserId,
    @UserPrincipalName,
    @TimestampUtc,
    @IpAddress,
    @Country,
    @City,
    @DeviceId,
    @DeviceName,
    @ClientApp,
    @ClientResource,
    @AuthMethod,
    @Status,
    @ConditionalAccess,
    @Error,
    @Result,
    @RiskLevel,
    @RiskEventTypesJson,
    @RawJson,
    @IsIgnored,
    @CreatedAt
);
";

    private readonly ISqlConnectionFactory _connectionFactory;

    public SimEventRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = ClearSql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertAsync(AccessEvent accessEvent, CancellationToken cancellationToken = default)
    {
        var id = accessEvent.Id == Guid.Empty ? Guid.NewGuid() : accessEvent.Id;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;

        AddParameter(command, "@Id", id);
        AddParameter(command, "@EventId", accessEvent.EventId);
        AddParameter(command, "@UserId", accessEvent.UserId);
        AddParameter(command, "@UserPrincipalName", accessEvent.UserPrincipalName);
        AddParameter(command, "@TimestampUtc", accessEvent.TimestampUtc);
        AddParameter(command, "@IpAddress", accessEvent.IpAddress);
        AddParameter(command, "@Country", accessEvent.Country);
        AddParameter(command, "@City", accessEvent.City);
        AddParameter(command, "@DeviceId", accessEvent.DeviceId);
        AddParameter(command, "@DeviceName", accessEvent.DeviceName);
        AddParameter(command, "@ClientApp", accessEvent.ClientApp);
        AddParameter(command, "@ClientResource", accessEvent.ClientResource);
        AddParameter(command, "@AuthMethod", accessEvent.AuthMethod);
        AddParameter(command, "@Status", accessEvent.Status);
        AddParameter(command, "@ConditionalAccess", accessEvent.ConditionalAccess);
        AddParameter(command, "@Error", accessEvent.Error);
        AddParameter(command, "@Result", accessEvent.Result);
        AddParameter(command, "@RiskLevel", accessEvent.RiskLevel);
        AddParameter(command, "@RiskEventTypesJson", accessEvent.RiskEventTypesJson);
        AddParameter(command, "@RawJson", accessEvent.RawJson);
        AddParameter(command, "@IsIgnored", accessEvent.IsIgnored);
        AddParameter(command, "@CreatedAt", accessEvent.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
