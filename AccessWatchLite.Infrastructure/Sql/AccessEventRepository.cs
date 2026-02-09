using System.Data.Common;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class AccessEventRepository : IAccessEventRepository
{
    private const string GetUnanalyzedSimSql = @"
SELECT TOP(@BatchSize) Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, DeviceId, DeviceName, ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, RawJson
FROM dbo.sim_Events
WHERE IsAnalyzed = 0
ORDER BY TimestampUtc ASC;
";

    private const string GetUnanalyzedRealSql = @"
SELECT TOP(@BatchSize) Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, DeviceId, DeviceName, ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, RawJson
FROM dbo.access_Events
WHERE IsAnalyzed = 0
ORDER BY TimestampUtc ASC;
";

    private const string GetRecentByUserSimSql = @"
SELECT Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, DeviceId, DeviceName, ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, RawJson
FROM dbo.sim_Events
WHERE UserId = @UserId AND TimestampUtc >= @Since
ORDER BY TimestampUtc DESC;
";

    private const string GetRecentByUserRealSql = @"
SELECT Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, DeviceId, DeviceName, ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, RawJson
FROM dbo.access_Events
WHERE UserId = @UserId AND TimestampUtc >= @Since
ORDER BY TimestampUtc DESC;
";

    private const string MarkAsAnalyzedSimSql = @"
UPDATE dbo.sim_Events
SET IsAnalyzed = 1, AnalyzedAt = @AnalyzedAt
WHERE Id = @Id;
";

    private const string MarkAsAnalyzedRealSql = @"
UPDATE dbo.access_Events
SET IsAnalyzed = 1, AnalyzedAt = @AnalyzedAt
WHERE Id = @Id;
";

    private const string CountFailedSimSql = @"
SELECT COUNT(*)
FROM dbo.sim_Events
WHERE UserId = @UserId AND Result = 'Failure' AND TimestampUtc >= @Since;
";

    private const string CountFailedRealSql = @"
SELECT COUNT(*)
FROM dbo.access_Events
WHERE UserId = @UserId AND Result = 'Failure' AND TimestampUtc >= @Since;
";

    private const string InsertRealSql = @"
INSERT INTO dbo.access_Events
(
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City,
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, RiskEventTypesJson, RawJson, IsIgnored, CreatedAt
)
VALUES
(
    @Id, @EventId, @UserId, @UserPrincipalName, @TimestampUtc, @IpAddress, @Country, @City,
    @DeviceId, @DeviceName, @ClientApp, @ClientResource, @AuthMethod, @Status, @ConditionalAccess,
    @Error, @Result, @RiskLevel, @RiskEventTypesJson, @RawJson, @IsIgnored, @CreatedAt
);
";

    private const string InsertSimSql = @"
INSERT INTO dbo.sim_Events
(
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City,
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, RiskEventTypesJson, RawJson, IsIgnored, CreatedAt
)
VALUES
(
    @Id, @EventId, @UserId, @UserPrincipalName, @TimestampUtc, @IpAddress, @Country, @City,
    @DeviceId, @DeviceName, @ClientApp, @ClientResource, @AuthMethod, @Status, @ConditionalAccess,
    @Error, @Result, @RiskLevel, @RiskEventTypesJson, @RawJson, @IsIgnored, @CreatedAt
);
";

    private const string GetRecentSimSql = @"
SELECT TOP(@Count) 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, 
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, CreatedAt
FROM dbo.sim_Events
ORDER BY CreatedAt DESC;
";

    private const string GetRecentRealSql = @"
SELECT TOP(@Count) 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, 
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, CreatedAt
FROM dbo.access_Events
ORDER BY CreatedAt DESC;
";

    private const string GetEventsSimSql = @"
SELECT TOP(@Count) 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, 
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, CreatedAt
FROM dbo.sim_Events
ORDER BY CreatedAt DESC;
";

    private const string GetEventsRealSql = @"
SELECT TOP(@Count) 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, IpAddress, Country, City, 
    DeviceId, DeviceName, ClientApp, ClientResource, AuthMethod, Status, ConditionalAccess,
    Error, Result, RiskLevel, CreatedAt
FROM dbo.access_Events
ORDER BY CreatedAt DESC;
";

    private readonly ISqlConnectionFactory _connectionFactory;

    public AccessEventRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<AccessEvent>> GetUnanalyzedEventsAsync(bool isSimulation, int batchSize = 100, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? GetUnanalyzedSimSql : GetUnanalyzedRealSql;
        AddParameter(command, "@BatchSize", batchSize);

        var events = new List<AccessEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(MapAccessEvent(reader));
        }

        return events;
    }

    public async Task<List<AccessEvent>> GetRecentEventsByUserAsync(string userId, bool isSimulation, DateTime since, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? GetRecentByUserSimSql : GetRecentByUserRealSql;
        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@Since", since);

        var events = new List<AccessEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(MapAccessEvent(reader));
        }

        return events;
    }

    public async Task MarkAsAnalyzedAsync(Guid eventId, bool isSimulation, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? MarkAsAnalyzedSimSql : MarkAsAnalyzedRealSql;
        AddParameter(command, "@Id", eventId);
        AddParameter(command, "@AnalyzedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> CountFailedAttemptsAsync(string userId, bool isSimulation, DateTime since, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? CountFailedSimSql : CountFailedRealSql;
        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@Since", since);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task InsertAsync(AccessEvent accessEvent, bool isSimulation, CancellationToken cancellationToken = default)
    {
        var id = accessEvent.Id == Guid.Empty ? Guid.NewGuid() : accessEvent.Id;
        
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? InsertSimSql : InsertRealSql;

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

    public async Task<List<AccessEvent>> GetRecentAsync(bool isSimulation, int count = 5, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? GetRecentSimSql : GetRecentRealSql;
        AddParameter(command, "@Count", count);

        var events = new List<AccessEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(MapAccessEventWithDetails(reader));
        }

        return events;
    }

    public async Task<List<AccessEvent>> GetEventsAsync(bool isSimulation, int count = 100, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = isSimulation ? GetEventsSimSql : GetEventsRealSql;
        AddParameter(command, "@Count", count);

        var events = new List<AccessEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(MapAccessEventWithDetails(reader));
        }

        return events;
    }

    private static AccessEvent MapAccessEvent(DbDataReader reader)
    {
        return new AccessEvent
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            EventId = reader.GetString(reader.GetOrdinal("EventId")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            UserPrincipalName = reader.GetString(reader.GetOrdinal("UserPrincipalName")),
            TimestampUtc = reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
            IpAddress = reader.GetString(reader.GetOrdinal("IpAddress")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader.GetString(reader.GetOrdinal("DeviceId")),
            DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName")) ? null : reader.GetString(reader.GetOrdinal("DeviceName")),
            ClientApp = reader.IsDBNull(reader.GetOrdinal("ClientApp")) ? null : reader.GetString(reader.GetOrdinal("ClientApp")),
            AuthMethod = reader.IsDBNull(reader.GetOrdinal("AuthMethod")) ? null : reader.GetString(reader.GetOrdinal("AuthMethod")),
            Result = reader.GetString(reader.GetOrdinal("Result")),
            RiskLevel = reader.IsDBNull(reader.GetOrdinal("RiskLevel")) ? null : reader.GetString(reader.GetOrdinal("RiskLevel")),
            RiskEventTypesJson = reader.IsDBNull(reader.GetOrdinal("RiskEventTypesJson")) ? null : reader.GetString(reader.GetOrdinal("RiskEventTypesJson")),
            RawJson = reader.IsDBNull(reader.GetOrdinal("RawJson")) ? null : reader.GetString(reader.GetOrdinal("RawJson"))
        };
    }

    private static AccessEvent MapAccessEventWithDetails(DbDataReader reader)
    {
        return new AccessEvent
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            EventId = reader.GetString(reader.GetOrdinal("EventId")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            UserPrincipalName = reader.GetString(reader.GetOrdinal("UserPrincipalName")),
            TimestampUtc = reader.GetDateTime(reader.GetOrdinal("TimestampUtc")),
            IpAddress = reader.GetString(reader.GetOrdinal("IpAddress")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader.GetString(reader.GetOrdinal("DeviceId")),
            DeviceName = reader.IsDBNull(reader.GetOrdinal("DeviceName")) ? null : reader.GetString(reader.GetOrdinal("DeviceName")),
            ClientApp = reader.IsDBNull(reader.GetOrdinal("ClientApp")) ? null : reader.GetString(reader.GetOrdinal("ClientApp")),
            ClientResource = reader.IsDBNull(reader.GetOrdinal("ClientResource")) ? null : reader.GetString(reader.GetOrdinal("ClientResource")),
            AuthMethod = reader.IsDBNull(reader.GetOrdinal("AuthMethod")) ? null : reader.GetString(reader.GetOrdinal("AuthMethod")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? null : reader.GetString(reader.GetOrdinal("Status")),
            ConditionalAccess = reader.IsDBNull(reader.GetOrdinal("ConditionalAccess")) ? null : reader.GetString(reader.GetOrdinal("ConditionalAccess")),
            Error = reader.IsDBNull(reader.GetOrdinal("Error")) ? null : reader.GetString(reader.GetOrdinal("Error")),
            Result = reader.GetString(reader.GetOrdinal("Result")),
            RiskLevel = reader.IsDBNull(reader.GetOrdinal("RiskLevel")) ? null : reader.GetString(reader.GetOrdinal("RiskLevel")),
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
