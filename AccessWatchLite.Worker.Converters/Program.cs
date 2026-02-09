using Azure.Identity;
using Azure.Storage.Blobs;
using AccessWatchLite.Infrastructure;
using AccessWatchLite.Worker.Converters;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddAzureKeyVault(
    new Uri("https://kv-tfm-security.vault.azure.net/"),
    new DefaultAzureCredential());

builder.Services.AddInfrastructure();

builder.Services.AddSingleton(_ =>
{
    var connectionString = builder.Configuration["BlobStorage:ConnectionString"];
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Missing BlobStorage:ConnectionString configuration.");
    }

    return new BlobServiceClient(connectionString);
});

builder.Services.AddHostedService<SimLoaderWorker>();

var host = builder.Build();
host.Run();
