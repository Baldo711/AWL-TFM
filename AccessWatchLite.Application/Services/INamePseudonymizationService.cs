namespace AccessWatchLite.Application.Services;

/// <summary>
/// Servicio para pseudonimización consistente de nombres
/// </summary>
public interface INamePseudonymizationService
{
    /// <summary>
    /// Obtiene un pseudónimo consistente para un nombre real
    /// Si el nombre ya tiene un mapeo, devuelve el existente
    /// Si no, genera uno nuevo y lo persiste
    /// </summary>
    Task<(string fullName, string email)> GetPseudonymAsync(string originalName, CancellationToken cancellationToken = default);
}
