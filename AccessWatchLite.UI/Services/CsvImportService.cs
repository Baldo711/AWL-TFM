using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Components.Forms;

namespace AccessWatchLite.UI.Services;

public sealed class CsvImportService
{
    private static readonly string[] RequiredHeaders =
    [
        "Fecha (UTC)",
        "Id. de solicitud",
        "Id. de usuario",
        "Nombre de usuario",
        "Dirección IP",
        "Ubicación",
        "Id. de dispositivo",
        "Explorador",
        "Aplicación cliente",
        "Protocolo de autenticación",
        "Estado"
    ];

    private static readonly string[] KnownHeaders =
    [
        "Fecha (UTC)",
        "Id. de solicitud",
        "Agente de usuario",
        "Id. de correlación",
        "Id. de usuario",
        "Usuario",
        "Nombre de usuario",
        "Tipo de usuario",
        "Tipo de acceso entre inquilinos",
        "Tipo de token entrante",
        "Protocolo de autenticación",
        "Identificador de token único",
        "Método de transferencia original",
        "Tipo de credencial de cliente",
        "Protección de token: sesión de inicio de sesión",
        "Protección con tokens: código de estado de sesión de inicio de sesión",
        "Aplicación",
        "Id. de aplicación",
        "Id. de inquilino del propietario de la aplicación",
        "Recurso",
        "Id. de recurso",
        "Id. de inquilino de recursos",
        "Id. de inquilino del administrador del recurso",
        "Id. de inquilino inicial",
        "Nombre del inquilino inicial",
        "Dirección IP",
        "Ubicación",
        "Estado",
        "Código de error de inicio de sesión",
        "Motivo del error",
        "Aplicación cliente",
        "Id. de dispositivo",
        "Explorador",
        "Sistema operativo",
        "Conforme",
        "Administrada",
        "Tipo de unión",
        "Resultado de la autenticación multifactor",
        "Método de autenticación de la autenticación multifactor",
        "Detalle de autenticación de la autenticación multifactor",
        "Requisito de autenticación",
        "Identificador de inicio de sesión",
        "Id. de sesión",
        "Dirección IP (vista por recurso)",
        "Mediante Acceso global seguro",
        "Dirección IP de Acceso global seguro",
        "Número de sistema autónomo",
        "Marcado para revisión",
        "Tipo de emisor de tokens",
        "Tipo de token entrante_1",
        "Nombre del emisor de tokens",
        "Latencia",
        "Acceso condicional",
        "Tipo de identidad administrada",
        "Id. del recurso asociado",
        "Id. de token federado",
        "Emisor de token federado"
    ];

    private readonly IConfiguration _config;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(IConfiguration config, ILogger<CsvImportService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<CsvUploadResult> UploadAndTestSqlAsync(IBrowserFile file, CancellationToken ct)
    {
        if (file is null || file.Size == 0)
            return new CsvUploadResult(false, "El archivo está vacío.", null, null, null, null);

        if (!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return new CsvUploadResult(false, "Solo se permiten archivos .csv", null, null, null, null);

        var blobConn = _config["BlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(blobConn))
            return new CsvUploadResult(false, "Falta el secreto 'BlobStorage--ConnectionString' (mapeado a 'BlobStorage:ConnectionString') en Key Vault.", null, null, null, null);

        // Blob: container = sim, path virtual = upfiles/...
        const string containerName = "sim";

        var blobServiceClient = new BlobServiceClient(blobConn);
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        var safeFileName = Path.GetFileName(file.Name);
        var uploadBlobName = $"upfiles/{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeFileName}";
        var uploadBlobClient = container.GetBlobClient(uploadBlobName);

        _logger.LogInformation("Subiendo CSV a Blob: {Container}/{BlobName}", containerName, uploadBlobName);

        await using (var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024, cancellationToken: ct))
        {
            await uploadBlobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        }

        _logger.LogInformation("CSV subido correctamente: {Uri}", uploadBlobClient.Uri);

        var (isValid, validationMessage, comparison) = await ValidateCsvFormatAsync(file, ct);
        if (!isValid)
        {
            return new CsvUploadResult(false, validationMessage, uploadBlobClient.Uri.ToString(), comparison, null, null);
        }

        var loaderBlobName = uploadBlobName.Replace("upfiles/", "loader/", StringComparison.OrdinalIgnoreCase);
        var loaderBlobClient = container.GetBlobClient(loaderBlobName);
        await loaderBlobClient.StartCopyFromUriAsync(uploadBlobClient.Uri, cancellationToken: ct);

        _logger.LogInformation("CSV copiado a loader: {Uri}", loaderBlobClient.Uri);

        // Calcular el nombre del log que se generará después del procesamiento
        // Debe coincidir con GetLogName() de SimLoaderFunction
        var fileName = Path.GetFileName(uploadBlobName);
        var logBlobName = $"logs/load-{fileName}.log";
        var logBlobClient = container.GetBlobClient(logBlobName);

        // Generar SAS URL para el log (válida por 24 horas)
        var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = logBlobName,
            Resource = "b", // b = blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // 5 min antes por si hay diferencias de reloj
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(24) // Válida por 24 horas
        };
        sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read); // Solo lectura

        // Generar la URL con SAS
        var sasUri = logBlobClient.GenerateSasUri(sasBuilder);

        return new CsvUploadResult(
            true, 
            $"OK: CSV subido a Blob ({uploadBlobClient.Uri}), validado y copiado a loader.", 
            uploadBlobClient.Uri.ToString(), 
            comparison,
            sasUri.ToString(),
            logBlobName); // Agregar nombre del blob del log
    }

    private static async Task<(bool ok, string message, CsvHeaderComparison comparison)> ValidateCsvFormatAsync(IBrowserFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024, cancellationToken: ct);
        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(ct);

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            return (false, "El archivo no contiene cabecera.", new CsvHeaderComparison(0, 0, RequiredHeaders.Length, Array.Empty<string>(), RequiredHeaders));
        }

        if (!headerLine.Contains(';', StringComparison.Ordinal))
        {
            return (false, "El archivo debe usar separador ';'.", new CsvHeaderComparison(0, 0, RequiredHeaders.Length, Array.Empty<string>(), RequiredHeaders));
        }

        var headers = headerLine.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var missingHeaders = RequiredHeaders
            .Where(required => !headers.Contains(required, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var extraHeaders = headers
            .Where(header => !KnownHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var matchingHeaders = headers
            .Where(header => KnownHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var comparison = new CsvHeaderComparison(matchingHeaders, extraHeaders.Length, missingHeaders.Length, extraHeaders, missingHeaders);

        if (missingHeaders.Length > 0)
        {
            return (false, $"Faltan cabeceras requeridas: {string.Join(", ", missingHeaders)}", comparison);
        }

        return (true, "Formato CSV válido.", comparison);
    }

    /// <summary>
    /// Verifica si el archivo LOG fue creado por SimLoaderFunction (indica procesamiento completado)
    /// </summary>
    public async Task<bool> IsProcessingCompleteAsync(string logBlobName, CancellationToken ct = default)
    {
        try
        {
            var blobConn = _config["BlobStorage:ConnectionString"];
            if (string.IsNullOrWhiteSpace(blobConn))
                return false;

            const string containerName = "sim";
            var blobServiceClient = new BlobServiceClient(blobConn);
            var container = blobServiceClient.GetBlobContainerClient(containerName);
            var logBlobClient = container.GetBlobClient(logBlobName);

            return await logBlobClient.ExistsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando si el procesamiento está completo para {LogBlobName}", logBlobName);
            return false;
        }
    }
}

public sealed record CsvHeaderComparison(
    int MatchingCount,
    int ExtraCount,
    int MissingCount,
    IReadOnlyCollection<string> ExtraHeaders,
    IReadOnlyCollection<string> MissingHeaders);

public sealed record CsvUploadResult(
bool Ok,
string Message,
string? BlobUri,
CsvHeaderComparison? HeaderComparison,
string? LogUri,
string? LogBlobName); // Nombre del blob del log (para polling)
