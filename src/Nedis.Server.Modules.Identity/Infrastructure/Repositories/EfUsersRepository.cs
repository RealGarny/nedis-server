using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Identity.Infrastructure.Data;
using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Repositories;

class EfUsersRepository(IdentityDbContext db) : IUsersRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByLoginAsync(string login, CancellationToken ct = default) =>
        db.Users.Include(u => u.LocalCredential).FirstOrDefaultAsync(u => u.Login == login, ct);

    public Task<User?> GetByGlobalIdentityAsync(string issuer, string subject, CancellationToken ct = default) =>
        db.GlobalIdentities
        .Where(g => g.Issuer == issuer && g.Subject == subject)
        .Select(g => g.User)
        .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) => db.Users.AnyAsync(u => u.Id == id, ct);

    public User Add(User user) => db.Users.Add(user).Entity;

    public void Update(User user) => db.Users.Update(user);

    public void Remove(User user) => db.Users.Remove(user);

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}