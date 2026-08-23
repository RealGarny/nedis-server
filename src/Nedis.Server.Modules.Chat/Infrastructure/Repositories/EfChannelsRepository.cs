using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Chat.Domain;
using Nedis.Server.Modules.Chat.Infrastructure.Data;

namespace Nedis.Server.Modules.Chat.Infrastructure.Repositories;

internal class EfChannelsRepository(ChatDbContext db) : IChannelsRepository
{
    public Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Channels.FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<Channel?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Channels.FirstOrDefaultAsync(c => c.Name == name, ct);
    public Task<List<Channel>> GetAllAsync(CancellationToken ct = default) =>
        db.Channels.OrderBy(c => c.Name).ToListAsync(ct);
    public Channel Add(Channel channel) => db.Channels.Add(channel).Entity;
    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}