using System.Net;
using AccessWatchLite.Application.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Functions.Functions;

/// <summary>
/// HTTP trigger para obtener el progreso actual del análisis
/// </summary>
public sealed class GetAnalysisProgressFunction
{
    private readonly IAnalysisProgressService _progressService;
    private readonly ILogger<GetAnalysisProgressFunction> _logger;

    public GetAnalysisProgressFunction(
        IAnalysisProgressService progressService,
        ILogger<GetAnalysisProgressFunction> logger)
    {
        _progressService = progressService;
        _logger = logger;
    }

    [Function(nameof(GetAnalysisProgressFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
        FunctionContext context)
    {
        _logger.LogInformation("Getting analysis progress");

        try
        {
            var progress = _progressService.GetCurrentProgress();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(progress);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis progress");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { error = ex.Message });
            return response;
        }
    }
}
