
using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Repositories;

public interface ISessionsRepository
{
    Task<Session?> GetWithUserAsync(Guid id, CancellationToken ct = default);

    void Add(Session session);

    Task RevokeAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct = default);

    Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}