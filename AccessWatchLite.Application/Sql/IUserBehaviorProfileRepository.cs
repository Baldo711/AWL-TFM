using AccessWatchLite.Application.Detection;

namespace AccessWatchLite.Application.Sql;

/// <summary>
/// Repositorio para construir y gestionar perfiles de comportamiento de usuarios
/// </summary>
public interface IUserBehaviorProfileRepository
{
    /// <summary>
    /// Construye un perfil de comportamiento para un usuario basado en su historial
    /// </summary>
    Task<UserBehaviorProfile> BuildProfileAsync(
        string userId,
        bool isSimulation,
        DateTime since, 
        CancellationToken cancellationToken = default);
}
