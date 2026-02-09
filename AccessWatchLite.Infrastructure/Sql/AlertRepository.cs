using System.Data.Common;
using System.Text.Json;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class AlertRepository : IAlertRepository
{
    private const string GetByIdSql = @"
SELECT Id, EventId, UserId, UserPrincipalName, IsSimulation, Severity, RiskScore, Status, Title, Description, DetectedSignals, EventTimestamp, IpAddress, Country, City, DeviceId, DetectedAt, ResolvedAt, ResolvedBy, Resolution
FROM dbo.alerts
WHERE Id = @Id;
";

    private const string GetRecentAlertsSql = @"
SELECT TOP(@Count) Id, EventId, UserId, UserPrincipalName, IsSimulation, Severity, RiskScore, Status, Title, Description, DetectedSignals, EventTimestamp, IpAddress, Country, City, DeviceId, DetectedAt, ResolvedAt, ResolvedBy, Resolution
FROM dbo.alerts
WHERE IsSimulation = @IsSimulation
ORDER BY DetectedAt DESC;
";

    private const string GetPendingAlertsSql = @"
SELECT Id, EventId, UserId, UserPrincipalName, IsSimulation, Severity, RiskScore, Status, Title, Description, DetectedSignals, EventTimestamp, IpAddress, Country, City, DeviceId, DetectedAt, ResolvedAt, ResolvedBy, Resolution
FROM dbo.alerts
WHERE IsSimulation = @IsSimulation AND Status IN ('New', 'Investigating')
ORDER BY RiskScore DESC, DetectedAt ASC;
";

    private const string InsertSql = @"
INSERT INTO dbo.alerts
(Id, EventId, UserId, UserPrincipalName, IsSimulation, Severity, RiskScore, Status, Title, Description, DetectedSignals, EventTimestamp, IpAddress, Country, City, DeviceId, DetectedAt)
VALUES
(@Id, @EventId, @UserId, @UserPrincipalName, @IsSimulation, @Severity, @RiskScore, @Status, @Title, @Description, @DetectedSignals, @EventTimestamp, @IpAddress, @Country, @City, @DeviceId, @DetectedAt);
";

    private const string UpdateStatusSql = @"
UPDATE dbo.alerts
SET Status = @Status, Resolution = @Resolution, UpdatedAt = @UpdatedAt
WHERE Id = @Id;
";

    private readonly ISqlConnectionFactory _connectionFactory;

    public AlertRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Alert?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetByIdSql;
        AddParameter(command, "@Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapAlert(reader);
        }

        return null;
    }

    public async Task<List<Alert>> GetRecentAlertsAsync(bool isSimulation, int count = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetRecentAlertsSql;
        AddParameter(command, "@Count", count);
        AddParameter(command, "@IsSimulation", isSimulation);

        var alerts = new List<Alert>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            alerts.Add(MapAlert(reader));
        }

        return alerts;
    }

    public async Task<List<Alert>> GetPendingAlertsAsync(bool isSimulation, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetPendingAlertsSql;
        AddParameter(command, "@IsSimulation", isSimulation);

        var alerts = new List<Alert>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            alerts.Add(MapAlert(reader));
        }

        return alerts;
    }

    public async Task InsertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;

        var detectedSignalsJson = alert.DetectedSignals != null 
            ? alert.DetectedSignals
            : null;

        AddParameter(command, "@Id", alert.Id);
        AddParameter(command, "@EventId", alert.EventId);
        AddParameter(command, "@UserId", alert.UserId);
        AddParameter(command, "@UserPrincipalName", alert.UserPrincipalName);
        AddParameter(command, "@IsSimulation", alert.IsSimulation);
        AddParameter(command, "@Severity", alert.Severity);
        AddParameter(command, "@RiskScore", alert.RiskScore);
        AddParameter(command, "@Status", alert.Status);
        AddParameter(command, "@Title", alert.Title);
        AddParameter(command, "@Description", alert.Description);
        AddParameter(command, "@DetectedSignals", detectedSignalsJson);
        AddParameter(command, "@EventTimestamp", alert.EventTimestamp);
        AddParameter(command, "@IpAddress", alert.IpAddress);
        AddParameter(command, "@Country", alert.Country);
        AddParameter(command, "@City", alert.City);
        AddParameter(command, "@DeviceId", alert.DeviceId);
        AddParameter(command, "@DetectedAt", alert.DetectedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? resolution = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateStatusSql;

        AddParameter(command, "@Id", id);
        AddParameter(command, "@Status", status);
        AddParameter(command, "@Resolution", resolution);
        AddParameter(command, "@UpdatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Alert MapAlert(DbDataReader reader)
    {
        var detectedSignalsJson = reader.IsDBNull(reader.GetOrdinal("DetectedSignals"))
            ? null
            : reader.GetString(reader.GetOrdinal("DetectedSignals"));

        return new Alert
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            EventId = reader.GetGuid(reader.GetOrdinal("EventId")),
            UserId = reader.IsDBNull(reader.GetOrdinal("UserId")) ? null : reader.GetString(reader.GetOrdinal("UserId")),
            UserPrincipalName = reader.IsDBNull(reader.GetOrdinal("UserPrincipalName")) ? null : reader.GetString(reader.GetOrdinal("UserPrincipalName")),
            IsSimulation = reader.GetBoolean(reader.GetOrdinal("IsSimulation")),
            Severity = reader.GetString(reader.GetOrdinal("Severity")),
            RiskScore = reader.GetDecimal(reader.GetOrdinal("RiskScore")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            Title = reader.GetString(reader.GetOrdinal("Title")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            DetectedSignals = detectedSignalsJson,
            EventTimestamp = reader.GetDateTime(reader.GetOrdinal("EventTimestamp")),
            IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? null : reader.GetString(reader.GetOrdinal("IpAddress")),
            Country = reader.IsDBNull(reader.GetOrdinal("Country")) ? null : reader.GetString(reader.GetOrdinal("Country")),
            City = reader.IsDBNull(reader.GetOrdinal("City")) ? null : reader.GetString(reader.GetOrdinal("City")),
            DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader.GetString(reader.GetOrdinal("DeviceId")),
            DetectedAt = reader.GetDateTime(reader.GetOrdinal("DetectedAt")),
            ResolvedAt = reader.IsDBNull(reader.GetOrdinal("ResolvedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ResolvedAt")),
            ResolvedBy = reader.IsDBNull(reader.GetOrdinal("ResolvedBy")) ? null : reader.GetString(reader.GetOrdinal("ResolvedBy")),
            Resolution = reader.IsDBNull(reader.GetOrdinal("Resolution")) ? null : reader.GetString(reader.GetOrdinal("Resolution"))
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
