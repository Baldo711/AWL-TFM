using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Dapper;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Infrastructure.Sql;

/// <summary>
/// Repository for response_Actions table operations.
/// </summary>
public sealed class ResponseActionRepository : IResponseActionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<ResponseActionRepository> _logger;

    public ResponseActionRepository(
        ISqlConnectionFactory connectionFactory,
        ILogger<ResponseActionRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InsertAsync(ResponseAction action, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.response_Actions 
            (Id, AlertId, ActionType, ActionStatus, ExecutedAt, Result, ErrorMessage, IsSimulation, CreatedAt)
            VALUES 
            (@Id, @AlertId, @ActionType, @ActionStatus, @ExecutedAt, @Result, @ErrorMessage, @IsSimulation, @CreatedAt)";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        await connection.ExecuteAsync(sql, new
        {
            action.Id,
            action.AlertId,
            action.ActionType,
            action.ActionStatus,
            action.ExecutedAt,
            action.Result,
            action.ErrorMessage,
            action.IsSimulation,
            action.CreatedAt
        });

        _logger.LogDebug(
            "Inserted response action {ActionId} for alert {AlertId} (Type: {ActionType}, Status: {Status})",
            action.Id, action.AlertId, action.ActionType, action.ActionStatus);
    }

    public async Task<List<ResponseAction>> GetByAlertIdAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, AlertId, ActionType, ActionStatus, ExecutedAt, Result, ErrorMessage, IsSimulation, CreatedAt
            FROM dbo.response_Actions
            WHERE AlertId = @AlertId
            ORDER BY CreatedAt DESC";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        var actions = await connection.QueryAsync<ResponseAction>(sql, new { AlertId = alertId });

        return actions.AsList();
    }

    public async Task<List<ResponseAction>> GetPendingAsync(
        bool isSimulation,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, AlertId, ActionType, ActionStatus, ExecutedAt, Result, ErrorMessage, IsSimulation, CreatedAt
            FROM dbo.response_Actions
            WHERE ActionStatus = 'Pending'
              AND IsSimulation = @IsSimulation
            ORDER BY CreatedAt ASC";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        var actions = await connection.QueryAsync<ResponseAction>(sql, new { IsSimulation = isSimulation });

        _logger.LogDebug(
            "Retrieved {Count} pending response actions (IsSimulation: {IsSimulation})",
            actions.Count(), isSimulation);

        return actions.AsList();
    }

    public async Task UpdateStatusAsync(
        Guid id,
        string status,
        string? result,
        string? errorMessage,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.response_Actions
            SET ActionStatus = @Status,
                ExecutedAt = @ExecutedAt,
                Result = @Result,
                ErrorMessage = @ErrorMessage
            WHERE Id = @Id";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        
        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            ExecutedAt = DateTime.UtcNow,
            Result = result,
            ErrorMessage = errorMessage
        });

        if (rowsAffected == 0)
        {
            _logger.LogWarning("No response action found with Id {ActionId} to update status", id);
        }
        else
        {
            _logger.LogDebug(
                "Updated response action {ActionId} status to {Status}",
                id, status);
        }
    }
}
