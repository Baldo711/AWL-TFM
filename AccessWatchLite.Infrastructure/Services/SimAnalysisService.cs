using System.Net.Http.Json;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Infrastructure.Services;

public sealed class SimAnalysisService : ISimAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly ISimMetadataRepository _metadataRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SimAnalysisService> _logger;

    public SimAnalysisService(
        HttpClient httpClient,
        ISimMetadataRepository metadataRepository,
        IConfiguration configuration,
        ILogger<SimAnalysisService> logger)
    {
        _httpClient = httpClient;
        _metadataRepository = metadataRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AnalysisResult> TriggerAnalysisAsync(DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            // URL de la Azure Function (desde configuración o Key Vault)
            var functionUrl = _configuration["AzureFunctions:AnalyzeSimEventsUrl"] 
                ?? "http://localhost:7071/api/AnalyzeSimEventsFunction"; // Fallback para desarrollo local

            var request = new AnalysisRequest
            {
                StartDate = startDate,
                EndDate = endDate,
                BatchSize = 1000
            };

            _logger.LogInformation("Triggering analysis from {Start} to {End}", 
                startDate?.ToString("yyyy-MM-dd") ?? "beginning",
                endDate?.ToString("yyyy-MM-dd") ?? "end");

            var response = await _httpClient.PostAsJsonAsync(functionUrl, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnalysisResult>(cancellationToken: cancellationToken);
            return result ?? new AnalysisResult { Success = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering analysis");
            throw;
        }
    }

    public async Task<(DateTime? min, DateTime? max)> GetDateRangeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await _metadataRepository.GetCurrentAsync(cancellationToken);
            
            if (metadata == null || !metadata.HasData)
            {
                return (null, null);
            }

            return (metadata.MinDate, metadata.MaxDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting date range from metadata");
            return (null, null);
        }
    }

    public async Task<(bool hasData, (DateTime? min, DateTime? max) dateRange)> HasSimulationDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await _metadataRepository.GetCurrentAsync(cancellationToken);
            
            if (metadata == null)
            {
                return (false, (null, null));
            }

            return (metadata.HasData, (metadata.MinDate, metadata.MaxDate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking simulation data from metadata");
            return (false, (null, null));
        }
    }

    public async Task<AnalysisProgress> GetAnalysisProgressAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var functionUrl = _configuration["AzureFunctions:GetAnalysisProgressUrl"] 
                ?? "http://localhost:7071/api/GetAnalysisProgressFunction";

            var response = await _httpClient.GetAsync(functionUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var progress = await response.Content.ReadFromJsonAsync<AnalysisProgress>(cancellationToken: cancellationToken);
            return progress ?? new AnalysisProgress { IsRunning = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analysis progress");
            return new AnalysisProgress { IsRunning = false };
        }
    }
}
