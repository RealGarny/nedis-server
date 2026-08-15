using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Repositories;

public interface IUsersRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByLoginAsync(string login, CancellationToken ct = default);

    Task<User?> GetByGlobalIdentityAsync(string issuer, string subject, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    User Add(User user);

    Task<int> SaveChangesAsync();

    void Update(User user);

    void Remove(User user);
}