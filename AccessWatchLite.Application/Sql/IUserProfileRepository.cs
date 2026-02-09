using AccessWatchLite.Domain;

namespace AccessWatchLite.Application.Sql;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task UpdateStatisticsAsync(string userId, bool wasSuccessful, CancellationToken cancellationToken = default);
}
