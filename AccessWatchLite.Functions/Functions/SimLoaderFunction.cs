using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AccessWatchLite.Application.Services;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AccessWatchLite.Functions.Functions;

public sealed class SimLoaderFunction
{
    private const string ContainerName = "sim";
    private const string LoaderPrefix = "loader/";
    private const string LogsPrefix = "logs/";

    private readonly BlobContainerClient _container;
    private readonly ISimEventRepository _repository;
    private readonly ISimMetadataRepository _metadataRepository;
    private readonly INamePseudonymizationService _pseudonymizationService;
    private readonly ILogger<SimLoaderFunction> _logger;

    public SimLoaderFunction(
        BlobServiceClient blobServiceClient, 
        ISimEventRepository repository,
        ISimMetadataRepository metadataRepository,
        INamePseudonymizationService pseudonymizationService,
        ILogger<SimLoaderFunction> logger)
    {
        _container = blobServiceClient.GetBlobContainerClient(ContainerName);
        _repository = repository;
        _metadataRepository = metadataRepository;
        _pseudonymizationService = pseudonymizationService;
        _logger = logger;
    }


    /// <summary>
    /// Se ejecuta cada 1 minuto para buscar y procesar archivos CSV en sim/loader/
    /// Compatible con local y Azure (Linux/Windows)
    /// Cron: "0 */1 * * * *" = cada 1 minuto
    /// </summary>
    [Function(nameof(SimLoaderFunction))]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer, FunctionContext context)
    {
        _logger.LogInformation("SimLoaderFunction ejecutándose: {Time}", DateTime.UtcNow);

        try
        {
            // Buscar todos los archivos en sim/loader/ que NO tienen log procesado
            var blobs = _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, LoaderPrefix, default);
            var foundFiles = 0;
            var processedFiles = 0;

            await foreach (var blobItem in blobs)
            {
                foundFiles++;
                var blobClient = _container.GetBlobClient(blobItem.Name);
                var logClient = _container.GetBlobClient(GetLogName(blobItem.Name));

                // Si ya existe el log, significa que ya fue procesado
                if (await logClient.ExistsAsync())
                {
                    _logger.LogInformation("Archivo ya procesado (saltando): {BlobName}", blobItem.Name);
                    continue;
                }

                _logger.LogInformation("Procesando nuevo archivo: {BlobName}", blobItem.Name);

                try
                {
                    await ProcessBlobAsync(blobClient, CancellationToken.None);
                    processedFiles++;
                    _logger.LogInformation("? Archivo procesado exitosamente: {BlobName}", blobItem.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "? Error procesando archivo: {BlobName}", blobItem.Name);
                }
            }

            if (foundFiles == 0)
            {
                _logger.LogInformation("No se encontraron archivos en sim/loader/");
            }
            else if (processedFiles == 0)
            {
                _logger.LogInformation("Se encontraron {FoundFiles} archivos, pero todos ya fueron procesados", foundFiles);
            }
            else
            {
                _logger.LogInformation("Archivos procesados: {ProcessedFiles} de {FoundFiles}", processedFiles, foundFiles);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en SimLoaderFunction");
        }
    }


    private async Task ProcessBlobAsync(BlobClient blobClient, CancellationToken ct)
    {
        var logBuilder = new StringBuilder();
        var inserted = 0;
        var errors = 0;
        var lineNumber = 0;

        Log(logBuilder, $"Inicio de transferencia a BD: {blobClient.Name}");

        try
        {
            await _repository.ClearAsync(ct);
            Log(logBuilder, "? Tabla sim_Events limpiada exitosamente.");
            _logger.LogInformation("sim_Events table cleared successfully");
        }
        catch (Exception ex)
        {
            Log(logBuilder, $"? ERROR al limpiar tabla sim_Events: {ex.Message}");
            _logger.LogError(ex, "Failed to clear sim_Events table");
            throw;
        }

        var download = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
        await using var stream = download.Value.Content;
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            Log(logBuilder, "ERROR: Archivo sin cabecera.");
            await WriteLogAsync(blobClient.Name, logBuilder.ToString(), ct);
            await MarkProcessedAsync(blobClient, ct);
            return;
        }

        var headers = ParseCsvLine(headerLine);
        var headerLookup = BuildHeaderLookup(headers);


        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var values = ParseCsvLine(line);
                var accessEvent = await MapAccessEventAsync(values, headerLookup, ct);
                await _repository.InsertAsync(accessEvent, ct);
                inserted++;
            }
            catch (Exception ex)
            {
                errors++;
                Log(logBuilder, $"ERROR fila {lineNumber}: {ex.Message}");
            }
        }

        Log(logBuilder, $"Filas insertadas: {inserted}");
        Log(logBuilder, $"Errores: {errors}");
        Log(logBuilder, errors == 0
            ? "Fin de transferencia: OK"
            : "Fin de transferencia: con errores");

        // Actualizar metadatos de simulación
        if (inserted > 0)
        {
            await _metadataRepository.UpdateFromEventsAsync("SimLoaderFunction", ct);
            Log(logBuilder, "Metadatos actualizados.");
        }

        await WriteLogAsync(blobClient.Name, logBuilder.ToString(), ct);
        if (errors == 0)
        {
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
        }
        else
        {
            await MarkProcessedAsync(blobClient, ct);
        }
    }

    private static IReadOnlyDictionary<string, int> BuildHeaderLookup(IReadOnlyList<string> headers)
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            if (!lookup.ContainsKey(headers[i]))
            {
                lookup[headers[i]] = i;
            }
        }

        return lookup;
    }

    private async Task<AccessEvent> MapAccessEventAsync(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers, CancellationToken ct)
    {
        string? GetValue(string header)
        {
            return headers.TryGetValue(header, out var index) && index < values.Count
                ? values[index]
                : null;
        }

        var location = GetValue("Ubicación");
        var (city, country) = ParseLocation(location);
        
        // Obtener nombre original para pseudonimización
        var originalUserName = GetValue("Nombre de usuario");
        var (pseudonymName, pseudonymEmail) = await _pseudonymizationService.GetPseudonymAsync(originalUserName ?? "Usuario Desconocido", ct);

        // Obtener campos de estado y acceso condicional
        var status = GetValue("Estado");
        var conditionalAccess = GetValue("Acceso condicional");
        
        // Calcular Result según lógica: ambos "correcto" = CORRECTO, alguno "error" = ERROR, sino INDEFINIDO
        string calculatedResult;
        if (status?.Equals("correcto", StringComparison.OrdinalIgnoreCase) == true && 
            conditionalAccess?.Equals("correcto", StringComparison.OrdinalIgnoreCase) == true)
        {
            calculatedResult = "CORRECTO";
        }
        else if (status?.Equals("error", StringComparison.OrdinalIgnoreCase) == true || 
                 conditionalAccess?.Equals("error", StringComparison.OrdinalIgnoreCase) == true)
        {
            calculatedResult = "ERROR";
        }
        else
        {
            calculatedResult = "INDEFINIDO";
        }

        var accessEvent = new AccessEvent
        {
            Id = Guid.NewGuid(),
            EventId = Anonymize(GetValue("Id. de solicitud")) ?? string.Empty,
            UserId = Anonymize(GetValue("Id. de usuario")),
            UserPrincipalName = pseudonymEmail, // Email pseudonimizado consistente
            TimestampUtc = ParseDateTime(GetValue("Fecha (UTC)")),
            IpAddress = GetValue("Dirección IP") ?? string.Empty,
            Country = country ?? "Unknown",
            City = city ?? "Unknown",
            DeviceId = Anonymize(GetValue("Id. de dispositivo")),
            DeviceName = GetValue("Explorador"),
            ClientApp = GetValue("Aplicación cliente"),
            ClientResource = GetValue("Recurso"),
            AuthMethod = GetValue("Requisito de autenticación"),
            Status = status,
            ConditionalAccess = conditionalAccess,
            Error = GetValue("Motivo del error"),
            Result = calculatedResult,
            RiskLevel = null,
            RiskEventTypesJson = null,
            RawJson = BuildRawJson(values, headers),
            IsIgnored = false, // Default: no ignorado
            CreatedAt = GetSpainLocalTime() // Hora local de España
        };

        return accessEvent;
    }

    private static DateTime GetSpainLocalTime()
    {
        try
        {
            // Intentar con formato Windows (desarrollo local)
            var spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // En Linux/Azure, usar formato IANA
            var spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
        }
    }

    private static DateTime ParseDateTime(string? value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            // El CSV de Entra ID tiene columna "Fecha (UTC)", así que YA viene en UTC
            // Simplemente marcamos como UTC sin convertir
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        throw new FormatException("Fecha (UTC) inválida.");

    }

    private static (string? city, string? country) ParseLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return (null, null);
        }

        var parts = location.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return (null, null);
        }

        var city = parts[0];
        var country = parts.Length >= 2 ? parts[^1] : null;
        return (city, country);
    }

    private static string BuildRawJson(IReadOnlyList<string> values, IReadOnlyDictionary<string, int> headers)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            var index = headers[header.Key];
            data[header.Key] = index < values.Count ? values[index] : null;
        }

        return JsonSerializer.Serialize(data);
    }

    private static string? Anonymize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var salt = "AccessWatchLite";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{salt}:{value}"));
        return Convert.ToHexString(bytes);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        result.Add(current.ToString());
        return result;
    }

    private async Task WriteLogAsync(string blobName, string content, CancellationToken ct)
    {
        var logName = GetLogName(blobName);
        var logClient = _container.GetBlobClient(logName);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await logClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

    private static string GetLogName(string blobName)
    {
        var fileName = Path.GetFileName(blobName);
        return $"{LogsPrefix}load-{fileName}.log";
    }

    private static async Task MarkProcessedAsync(BlobClient blobClient, CancellationToken ct)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["processed"] = "true",
            ["processedAt"] = DateTime.UtcNow.ToString("O")
        };

        await blobClient.SetMetadataAsync(metadata, cancellationToken: ct);
    }

    private static void Log(StringBuilder builder, string message)
    {
        builder.AppendLine($"[{DateTime.UtcNow:O}] {message}");
    }
}
