using AccessWatchLite.Application.Response;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Infrastructure.Services;

/// <summary>
/// Service for orchestrating automated response actions on security alerts.
/// </summary>
public sealed class ResponseService : IResponseService
{
    private readonly ILogger<ResponseService> _logger;
    private readonly IEnumerable<IResponseAction> _responseActions;
    private readonly IResponseActionRepository _repository;

    public ResponseService(
        ILogger<ResponseService> logger,
        IEnumerable<IResponseAction> responseActions,
        IResponseActionRepository repository)
    {
        _logger = logger;
        _responseActions = responseActions;
        _repository = repository;
    }

    public async Task<int> ExecuteActionsForAlertAsync(
        Alert alert,
        List<string> actionTypes,
        CancellationToken cancellationToken = default)
    {
        var successCount = 0;

        _logger.LogInformation(
            "Executing {Count} response actions for alert {AlertId} (Severity: {Severity}, IsSimulation: {IsSimulation})",
            actionTypes.Count, alert.Id, alert.Severity, alert.IsSimulation);

        foreach (var actionType in actionTypes)
        {
            try
            {
                // Find the matching IResponseAction implementation
                var action = _responseActions.FirstOrDefault(a => a.ActionType == actionType);

                if (action == null)
                {
                    _logger.LogWarning("No IResponseAction implementation found for ActionType: {ActionType}", actionType);
                    
                    // Insert failed action record
                    await _repository.InsertAsync(new ResponseAction
                    {
                        Id = Guid.NewGuid(),
                        AlertId = alert.Id,
                        ActionType = actionType,
                        ActionStatus = "Failed",
                        ExecutedAt = DateTime.UtcNow,
                        ErrorMessage = $"No implementation found for ActionType: {actionType}",
                        IsSimulation = alert.IsSimulation,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);

                    continue;
                }

                // Insert pending action record
                var responseAction = new ResponseAction
                {
                    Id = Guid.NewGuid(),
                    AlertId = alert.Id,
                    ActionType = actionType,
                    ActionStatus = "Pending",
                    IsSimulation = alert.IsSimulation,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.InsertAsync(responseAction, cancellationToken);

                // Execute the action
                var result = await action.ExecuteAsync(alert, cancellationToken);

                // Update action status based on result
                await _repository.UpdateStatusAsync(
                    responseAction.Id,
                    result.Success ? "Executed" : "Failed",
                    result.Success ? result.Details ?? result.Message : null,
                    result.Success ? null : result.ErrorMessage ?? result.Message,
                    cancellationToken);

                if (result.Success)
                {
                    successCount++;
                    _logger.LogInformation(
                        "Action {ActionType} executed successfully for alert {AlertId}: {Message}",
                        actionType, alert.Id, result.Message);
                }
                else
                {
                    _logger.LogError(
                        "Action {ActionType} failed for alert {AlertId}: {Message}. Error: {Error}",
                        actionType, alert.Id, result.Message, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception executing action {ActionType} for alert {AlertId}", actionType, alert.Id);

                // Try to update action status to Failed
                try
                {
                    await _repository.InsertAsync(new ResponseAction
                    {
                        Id = Guid.NewGuid(),
                        AlertId = alert.Id,
                        ActionType = actionType,
                        ActionStatus = "Failed",
                        ExecutedAt = DateTime.UtcNow,
                        ErrorMessage = ex.Message,
                        IsSimulation = alert.IsSimulation,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
                catch (Exception insertEx)
                {
                    _logger.LogError(insertEx, "Failed to insert error record for action {ActionType}", actionType);
                }
            }
        }

        _logger.LogInformation(
            "Completed {SuccessCount}/{TotalCount} response actions for alert {AlertId}",
            successCount, actionTypes.Count, alert.Id);

        return successCount;
    }

    public async Task<List<ResponseAction>> GetPendingActionsAsync(
        bool isSimulation,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetPendingAsync(isSimulation, cancellationToken);
    }

    public async Task<List<ResponseAction>> GetActionsForAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByAlertIdAsync(alertId, cancellationToken);
    }
}
