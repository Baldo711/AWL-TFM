using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AccessWatchLite.Application.Sql;
using AccessWatchLite.Domain;

namespace AccessWatchLite.Functions.Functions;

/// <summary>
/// Función para ingerir eventos de acceso desde Microsoft Entra ID (Graph API)
/// y almacenarlos en access_Events (tabla REAL).
/// </summary>
public sealed class IngestionFunction
{
    private readonly ILogger<IngestionFunction> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAccessEventRepository _accessEventRepository;

    public IngestionFunction(
        ILogger<IngestionFunction> logger,
        IConfiguration configuration,
        IAccessEventRepository accessEventRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _accessEventRepository = accessEventRepository;
    }

    /// <summary>
    /// Se ejecuta cada 1 minuto para obtener eventos de Entra ID (rápido para testing)
    /// Cron: "0 * * * * *" = cada 1 minuto
    /// NOTA: Cambiar a "0 */5 * * * *" (cada 5 min) en producción para reducir llamadas a Graph API
    /// </summary>
    [Function(nameof(IngestionFunction))]
    public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timer, FunctionContext context)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("IngestionFunction iniciada en: {Time}", startTime);

        try
        {
            // Configuración de Entra ID (desde Key Vault)
            var tenantId = _configuration["EntraId:TenantId"];
            var clientId = _configuration["EntraId:ClientId"];
            var clientSecret = _configuration["EntraId:ClientSecret"];

            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogError("Configuración de Entra ID incompleta. Verifica EntraId:TenantId, EntraId:ClientId, EntraId:ClientSecret en Key Vault.");
                return;
            }

            _logger.LogInformation("? Configuración de Entra ID cargada correctamente");

            // Crear cliente Graph API con Client Credentials
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var graphClient = new GraphServiceClient(credential);

            // Obtener eventos desde los últimos 5 minutos (+ 1 min de margen)
            var since = DateTime.UtcNow.AddMinutes(-6);
            var sinceIso = since.ToString("yyyy-MM-ddTHH:mm:ssZ");

            _logger.LogInformation("Consultando sign-ins desde: {Since}", sinceIso);

            // Query a Graph API con filtro de fecha
            var signIns = await graphClient.AuditLogs.SignIns
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Filter = $"createdDateTime ge {sinceIso}";
                    requestConfiguration.QueryParameters.Top = 1000; // Límite de resultados
                    requestConfiguration.QueryParameters.Orderby = new[] { "createdDateTime desc" };
                });

            if (signIns?.Value == null || signIns.Value.Count == 0)
            {
                _logger.LogInformation("No se encontraron nuevos sign-ins desde {Since}", sinceIso);
                return;
            }

            _logger.LogInformation("Se encontraron {Count} sign-ins. Procesando...", signIns.Value.Count);

            var inserted = 0;
            var errors = 0;

            foreach (var signIn in signIns.Value)
            {
                try
                {
                    var accessEvent = MapSignInToAccessEvent(signIn);
                    await _accessEventRepository.InsertAsync(accessEvent, isSimulation: false);
                    inserted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al insertar evento {EventId}", signIn.Id);
                    errors++;
                }
            }

            var endTime = DateTime.UtcNow;
            var duration = (endTime - startTime).TotalSeconds;

            _logger.LogInformation(
                "? IngestionFunction completada. Insertados: {Inserted}, Errores: {Errors}, Duración: {Duration}s",
                inserted, errors, duration);

            // NO loguear datos sensibles (emails, IPs, IDs) - solo estadísticas
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en IngestionFunction");
        }
    }

    private AccessEvent MapSignInToAccessEvent(SignIn signIn)
    {
        // NO anonimizamos - guardamos datos REALES tal cual vienen de Entra ID
        // Si en el futuro se requiere seguridad, se implementará encriptación
        
        // Calcular Result basado en Status y ConditionalAccess
        var status = signIn.Status?.ErrorCode == 0 ? "correcto" : "error";
        var conditionalAccess = signIn.ConditionalAccessStatus?.ToString() ?? "unknown";
        var result = CalculateResult(status, conditionalAccess);
        
        var createdAt = GetSpainLocalTime();
        _logger.LogDebug("Evento {EventId} - CreatedAt (España): {CreatedAt}", 
            signIn.Id, 
            createdAt.ToString("yyyy-MM-dd HH:mm:ss"));

        return new AccessEvent
        {
            Id = Guid.NewGuid(),
            EventId = signIn.Id ?? Guid.NewGuid().ToString(), // ID real
            UserId = signIn.UserId, // ID real
            UserPrincipalName = signIn.UserPrincipalName, // Email real
            TimestampUtc = signIn.CreatedDateTime?.UtcDateTime ?? DateTime.UtcNow,
            IpAddress = signIn.IpAddress ?? "0.0.0.0",
            Country = signIn.Location?.CountryOrRegion,
            City = signIn.Location?.City,
            DeviceId = signIn.DeviceDetail?.DeviceId, // ID real
            DeviceName = signIn.DeviceDetail?.Browser,
            ClientApp = signIn.AppDisplayName,
            ClientResource = signIn.ResourceDisplayName,
            AuthMethod = null, // No disponible como campo simple en SDK v5
            Status = status,
            ConditionalAccess = conditionalAccess,
            Error = signIn.Status?.FailureReason,
            Result = result,
            RiskLevel = signIn.RiskLevelDuringSignIn?.ToString(),
            RiskEventTypesJson = null, // No disponible en SDK actual
            RawJson = System.Text.Json.JsonSerializer.Serialize(signIn), // JSON completo para auditoría
            IsIgnored = false,
            CreatedAt = createdAt // Hora local de España
        };
    }

    private DateTime GetSpainLocalTime()
    {
        TimeZoneInfo spainTimeZone;
        
        try
        {
            // Intentar con formato Windows (desarrollo local)
            spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            _logger.LogDebug("Usando timezone Windows: Romance Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // En Linux/Azure, usar formato IANA
                spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
                _logger.LogDebug("Usando timezone Linux/IANA: Europe/Madrid");
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "No se pudo encontrar timezone de España. Usando UTC+1 manualmente");
                // Fallback: Usar offset manual UTC+1 (o UTC+2 en verano)
                return DateTime.UtcNow.AddHours(1);
            }
        }
        
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, spainTimeZone);
        _logger.LogDebug("UTC: {Utc}, España: {Local}, Offset: {Offset}", 
            DateTime.UtcNow.ToString("HH:mm:ss"), 
            localTime.ToString("HH:mm:ss"),
            spainTimeZone.GetUtcOffset(DateTime.UtcNow).TotalHours);
        
        return localTime;
    }

    private static string CalculateResult(string status, string conditionalAccess)
    {
        // Lógica de Result según Status y ConditionalAccess
        if (status == "correcto")
        {
            return conditionalAccess.ToLower() switch
            {
                "success" => "CORRECTO",
                "failure" => "ERROR",
                "notapplied" => "CORRECTO",
                _ => "INDEFINIDO"
            };
        }
        else
        {
            return "ERROR";
        }
    }
}
