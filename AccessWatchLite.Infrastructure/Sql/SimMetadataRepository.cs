using System.Data.Common;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Infrastructure.Sql;

public sealed class SimMetadataRepository : ISimMetadataRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SimMetadataRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SimMetadata?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        const string sql = @"
            SELECT TOP 1 
                Id, HasData, MinDate, MaxDate, TotalEvents, UnanalyzedEvents, 
                LastUpdatedUtc, UpdatedBy
            FROM dbo.sim_Metadata
            ORDER BY Id DESC";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (await reader.ReadAsync(cancellationToken))
        {
            return new SimMetadata
            {
                Id = reader.GetInt32(0),
                HasData = reader.GetBoolean(1),
                MinDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                MaxDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                TotalEvents = reader.GetInt32(4),
                UnanalyzedEvents = reader.GetInt32(5),
                LastUpdatedUtc = reader.GetDateTime(6),
                UpdatedBy = reader.GetString(7)
            };
        }

        return null;
    }

    public async Task UpdateFromEventsAsync(string updatedBy, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        const string sql = @"
            -- Calcular estadísticas actuales de sim_Events
            DECLARE @HasData BIT = 0;
            DECLARE @MinDate DATETIME2 = NULL;
            DECLARE @MaxDate DATETIME2 = NULL;
            DECLARE @TotalEvents INT = 0;
            DECLARE @UnanalyzedEvents INT = 0;

            SELECT 
                @TotalEvents = COUNT(*),
                @UnanalyzedEvents = SUM(CASE WHEN IsAnalyzed = 0 THEN 1 ELSE 0 END),
                @MinDate = MIN(TimestampUtc),
                @MaxDate = MAX(TimestampUtc)
            FROM dbo.sim_Events;

            SET @HasData = CASE WHEN @TotalEvents > 0 THEN 1 ELSE 0 END;

            -- Actualizar o insertar metadata
            IF EXISTS (SELECT 1 FROM dbo.sim_Metadata)
            BEGIN
                UPDATE dbo.sim_Metadata
                SET 
                    HasData = @HasData,
                    MinDate = @MinDate,
                    MaxDate = @MaxDate,
                    TotalEvents = @TotalEvents,
                    UnanalyzedEvents = @UnanalyzedEvents,
                    LastUpdatedUtc = GETUTCDATE(),
                    UpdatedBy = @UpdatedBy
                WHERE Id = (SELECT TOP 1 Id FROM dbo.sim_Metadata ORDER BY Id DESC);
            END
            ELSE
            BEGIN
                INSERT INTO dbo.sim_Metadata (HasData, MinDate, MaxDate, TotalEvents, UnanalyzedEvents, LastUpdatedUtc, UpdatedBy)
                VALUES (@HasData, @MinDate, @MaxDate, @TotalEvents, @UnanalyzedEvents, GETUTCDATE(), @UpdatedBy);
            END";

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        var param = command.CreateParameter();
        param.ParameterName = "@UpdatedBy";
        param.Value = updatedBy;
        command.Parameters.Add(param);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
