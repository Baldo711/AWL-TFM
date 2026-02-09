using Azure.Identity;
using Azure.Storage.Blobs;
using AccessWatchLite.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
.ConfigureFunctionsWebApplication()
.ConfigureAppConfiguration((context, config) =>
{
    // Cargar Azure Key Vault SIEMPRE (desarrollo y producción)
    // DefaultAzureCredential usa automáticamente:
    // - Visual Studio account (desarrollo local)
    // - Managed Identity (Azure)
    config.AddAzureKeyVault(
        new Uri("https://kv-tfm-security.vault.azure.net/"),
        new DefaultAzureCredential());
})
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // HttpClient para servicios que lo requieren (aunque no se usen en Functions)
        services.AddHttpClient();

        // Infrastructure (SQL repositories)
        services.AddInfrastructure();

        // Blob Storage Client
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            // Azure Functions usa AzureWebJobsStorage por defecto
            var connectionString = config["AzureWebJobsStorage"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Missing AzureWebJobsStorage configuration.");
            }

            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
