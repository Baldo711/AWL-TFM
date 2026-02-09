using System.Data.Common;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private const string GetByUserIdSql = @"
SELECT Id, UserId, UserPrincipalName, UsualCountries, UsualCities, UsualIpRanges, KnownDevices, UsualSchedule, UsualAuthMethods, UsualClientApps, TotalAccessCount, FailedAccessCount, LastAccessDate, IsHighPrivilege, CustomRiskThreshold, CreatedAt, UpdatedAt
FROM dbo.user_Profiles
WHERE UserId = @UserId;
";

    private const string UpsertSql = @"
MERGE dbo.user_Profiles AS target
USING (SELECT @UserId AS UserId) AS source
ON target.UserId = source.UserId
WHEN MATCHED THEN
    UPDATE SET
        UserPrincipalName = @UserPrincipalName,
        UsualCountries = @UsualCountries,
        UsualCities = @UsualCities,
        UsualIpRanges = @UsualIpRanges,
        KnownDevices = @KnownDevices,
        UsualSchedule = @UsualSchedule,
        UsualAuthMethods = @UsualAuthMethods,
        UsualClientApps = @UsualClientApps,
        TotalAccessCount = @TotalAccessCount,
        FailedAccessCount = @FailedAccessCount,
        LastAccessDate = @LastAccessDate,
        IsHighPrivilege = @IsHighPrivilege,
        CustomRiskThreshold = @CustomRiskThreshold,
        UpdatedAt = @UpdatedAt
WHEN NOT MATCHED THEN
    INSERT (Id, UserId, UserPrincipalName, UsualCountries, UsualCities, UsualIpRanges, KnownDevices, UsualSchedule, UsualAuthMethods, UsualClientApps, TotalAccessCount, FailedAccessCount, LastAccessDate, IsHighPrivilege, CustomRiskThreshold, CreatedAt, UpdatedAt)
    VALUES (@Id, @UserId, @UserPrincipalName, @UsualCountries, @UsualCities, @UsualIpRanges, @KnownDevices, @UsualSchedule, @UsualAuthMethods, @UsualClientApps, @TotalAccessCount, @FailedAccessCount, @LastAccessDate, @IsHighPrivilege, @CustomRiskThreshold, @CreatedAt, @UpdatedAt);
";

    private const string UpdateStatisticsSql = @"
UPDATE dbo.user_Profiles
SET 
    TotalAccessCount = TotalAccessCount + 1,
    FailedAccessCount = FailedAccessCount + CASE WHEN @WasSuccessful = 0 THEN 1 ELSE 0 END,
    LastAccessDate = @LastAccessDate,
    UpdatedAt = @UpdatedAt
WHERE UserId = @UserId;
";

    private readonly ISqlConnectionFactory _connectionFactory;

    public UserProfileRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetByUserIdSql;
        AddParameter(command, "@UserId", userId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUserProfile(reader);
        }

        return null;
    }

    public async Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpsertSql;

        AddParameter(command, "@Id", profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id);
        AddParameter(command, "@UserId", profile.UserId);
        AddParameter(command, "@UserPrincipalName", profile.UserPrincipalName);
        AddParameter(command, "@UsualCountries", profile.UsualCountries);
        AddParameter(command, "@UsualCities", profile.UsualCities);
        AddParameter(command, "@UsualIpRanges", profile.UsualIpRanges);
        AddParameter(command, "@KnownDevices", profile.KnownDevices);
        AddParameter(command, "@UsualSchedule", profile.UsualSchedule);
        AddParameter(command, "@UsualAuthMethods", profile.UsualAuthMethods);
        AddParameter(command, "@UsualClientApps", profile.UsualClientApps);
        AddParameter(command, "@TotalAccessCount", profile.TotalAccessCount);
        AddParameter(command, "@FailedAccessCount", profile.FailedAccessCount);
        AddParameter(command, "@LastAccessDate", profile.LastAccessDate);
        AddParameter(command, "@IsHighPrivilege", profile.IsHighPrivilege);
        AddParameter(command, "@CustomRiskThreshold", profile.CustomRiskThreshold);
        AddParameter(command, "@CreatedAt", profile.CreatedAt);
        AddParameter(command, "@UpdatedAt", profile.UpdatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatisticsAsync(string userId, bool wasSuccessful, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateStatisticsSql;

        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@WasSuccessful", wasSuccessful);
        AddParameter(command, "@LastAccessDate", DateTime.UtcNow);
        AddParameter(command, "@UpdatedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static UserProfile MapUserProfile(DbDataReader reader)
    {
        return new UserProfile
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            UserId = reader.GetString(reader.GetOrdinal("UserId")),
            UserPrincipalName = reader.IsDBNull(reader.GetOrdinal("UserPrincipalName")) ? null : reader.GetString(reader.GetOrdinal("UserPrincipalName")),
            UsualCountries = reader.IsDBNull(reader.GetOrdinal("UsualCountries")) ? null : reader.GetString(reader.GetOrdinal("UsualCountries")),
            UsualCities = reader.IsDBNull(reader.GetOrdinal("UsualCities")) ? null : reader.GetString(reader.GetOrdinal("UsualCities")),
            UsualIpRanges = reader.IsDBNull(reader.GetOrdinal("UsualIpRanges")) ? null : reader.GetString(reader.GetOrdinal("UsualIpRanges")),
            KnownDevices = reader.IsDBNull(reader.GetOrdinal("KnownDevices")) ? null : reader.GetString(reader.GetOrdinal("KnownDevices")),
            UsualSchedule = reader.IsDBNull(reader.GetOrdinal("UsualSchedule")) ? null : reader.GetString(reader.GetOrdinal("UsualSchedule")),
            UsualAuthMethods = reader.IsDBNull(reader.GetOrdinal("UsualAuthMethods")) ? null : reader.GetString(reader.GetOrdinal("UsualAuthMethods")),
            UsualClientApps = reader.IsDBNull(reader.GetOrdinal("UsualClientApps")) ? null : reader.GetString(reader.GetOrdinal("UsualClientApps")),
            TotalAccessCount = reader.GetInt32(reader.GetOrdinal("TotalAccessCount")),
            FailedAccessCount = reader.GetInt32(reader.GetOrdinal("FailedAccessCount")),
            LastAccessDate = reader.IsDBNull(reader.GetOrdinal("LastAccessDate")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAccessDate")),
            IsHighPrivilege = reader.GetBoolean(reader.GetOrdinal("IsHighPrivilege")),
            CustomRiskThreshold = reader.IsDBNull(reader.GetOrdinal("CustomRiskThreshold")) ? null : reader.GetDecimal(reader.GetOrdinal("CustomRiskThreshold")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
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
