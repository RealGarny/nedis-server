using Nedis.Server.Modules.Chat.Domain;

namespace Nedis.Server.Modules.Chat.Infrastructure.Repositories;

public interface IChannelsRepository
{
    Task<Channel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Channel?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Channel>> GetAllAsync(CancellationToken ct = default);
    Channel Add(Channel channel);
    Task<int> SaveChangesAsync();
}
