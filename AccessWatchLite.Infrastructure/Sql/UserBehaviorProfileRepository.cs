using System.Data.Common;
using AccessWatchLite.Application.Detection;
using AccessWatchLite.Application.Sql;

namespace AccessWatchLite.Infrastructure.Sql;

/// <summary>
/// Implementación del repositorio de perfiles de comportamiento de usuarios
/// </summary>
public sealed class UserBehaviorProfileRepository : IUserBehaviorProfileRepository
{
    private static string BuildProfileSql(bool isSimulation)
    {
        var tableName = isSimulation ? "dbo.sim_Events" : "dbo.access_Events";
        
        return $@"
-- Obtener estadísticas generales
SELECT 
    @UserId AS UserId,
    COUNT(*) AS TotalAccessCount,
    SUM(CASE WHEN Result = 'CORRECTO' THEN 1 ELSE 0 END) AS SuccessfulAccessCount,
    SUM(CASE WHEN Result = 'ERROR' THEN 1 ELSE 0 END) AS FailedAccessCount
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since;

-- Top 5 países más frecuentes
SELECT TOP 5 Country, COUNT(*) AS Frequency
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since AND Country IS NOT NULL
GROUP BY Country
ORDER BY Frequency DESC;

-- Top 5 ciudades más frecuentes
SELECT TOP 5 City, COUNT(*) AS Frequency
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since AND City IS NOT NULL
GROUP BY City
ORDER BY Frequency DESC;

-- Últimas 20 IPs únicas
SELECT DISTINCT TOP 20 IpAddress
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since
ORDER BY CreatedAt DESC;

-- Dispositivos únicos conocidos
SELECT DISTINCT DeviceId
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since AND DeviceId IS NOT NULL;

-- Aplicaciones cliente más usadas
SELECT DISTINCT TOP 10 ClientApp
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since AND ClientApp IS NOT NULL;

-- Métodos de autenticación usados
SELECT DISTINCT AuthMethod
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since AND AuthMethod IS NOT NULL;

-- Horarios típicos por día de semana
SELECT 
    DATEPART(WEEKDAY, CreatedAt) AS DayOfWeek,
    MIN(CAST(CreatedAt AS TIME)) AS MinTime,
    MAX(CAST(CreatedAt AS TIME)) AS MaxTime,
    COUNT(*) AS AccessCount
FROM {tableName}
WHERE UserId = @UserId AND CreatedAt >= @Since
GROUP BY DATEPART(WEEKDAY, CreatedAt);
";
    }

    private readonly ISqlConnectionFactory _connectionFactory;

    public UserBehaviorProfileRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserBehaviorProfile> BuildProfileAsync(
        string userId,
        bool isSimulation,
        DateTime since, 
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = BuildProfileSql(isSimulation);
        
        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@Since", since);

        var profile = new UserBehaviorProfile
        {
            UserId = userId,
            ProfileBuiltAt = DateTime.UtcNow,
            ProfilePeriodStart = since,
            ProfilePeriodEnd = DateTime.UtcNow
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // 1. Estadísticas generales
        if (await reader.ReadAsync(cancellationToken))
        {
            profile.TotalAccessCount = reader.GetInt32(reader.GetOrdinal("TotalAccessCount"));
            profile.SuccessfulAccessCount = reader.GetInt32(reader.GetOrdinal("SuccessfulAccessCount"));
            profile.FailedAccessCount = reader.GetInt32(reader.GetOrdinal("FailedAccessCount"));
        }

        // 2. Top países
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                profile.CommonCountries.Add(reader.GetString(reader.GetOrdinal("Country")));
            }
        }

        // 3. Top ciudades
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                profile.CommonCities.Add(reader.GetString(reader.GetOrdinal("City")));
            }
        }

        // 4. IPs habituales
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                profile.CommonIps.Add(reader.GetString(reader.GetOrdinal("IpAddress")));
            }
        }

        // 5. Dispositivos conocidos
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var deviceId = reader.GetString(reader.GetOrdinal("DeviceId"));
                if (!string.IsNullOrEmpty(deviceId))
                {
                    profile.KnownDevices.Add(deviceId);
                }
            }
        }

        // 6. Aplicaciones comunes
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                profile.CommonApps.Add(reader.GetString(reader.GetOrdinal("ClientApp")));
            }
        }

        // 7. Métodos de autenticación
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var authMethod = reader.GetString(reader.GetOrdinal("AuthMethod"));
                if (!string.IsNullOrEmpty(authMethod))
                {
                    profile.UsualAuthMethods.Add(authMethod);
                }
            }
        }

        // 8. Horarios típicos por día de semana
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var dayOfWeek = (DayOfWeek)(reader.GetInt32(reader.GetOrdinal("DayOfWeek")) - 1); // SQL Sunday=1, C# Sunday=0
                
                // Convertir TIME a TimeSpan
                var minTimeValue = reader.GetValue(reader.GetOrdinal("MinTime"));
                var maxTimeValue = reader.GetValue(reader.GetOrdinal("MaxTime"));
                
                var minTime = minTimeValue is TimeSpan minTs ? minTs : TimeSpan.Zero;
                var maxTime = maxTimeValue is TimeSpan maxTs ? maxTs : TimeSpan.FromHours(23);
                
                profile.TypicalWorkingHours[dayOfWeek] = new TimeRange(minTime, maxTime);
            }
        }

        return profile;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
