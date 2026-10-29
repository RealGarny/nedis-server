using Microsoft.EntityFrameworkCore;

using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Data;

namespace Nedis.Server.Modules.Identity.Infrastructure.Repositories;

internal class EfSessionsRepository(IdentityDbContext db) : ISessionsRepository
{
    public Task<Session?> GetWithUserAsync(Guid id, CancellationToken ct = default) =>
        db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Add(Session session) =>
        db.Sessions.Add(session);

    public Task RevokeAsync(Guid sessionId, DateTimeOffset now, CancellationToken ct = default) =>
        db.Sessions.Where(s => s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(u =>
                u.SetProperty(s => s.RevokedAt, (DateTimeOffset?)now),
                ct
            );

    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default) =>
        db.Sessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ExecuteUpdateAsync(
                u => u.SetProperty(s => s.RevokedAt, now),
                ct
            );

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}