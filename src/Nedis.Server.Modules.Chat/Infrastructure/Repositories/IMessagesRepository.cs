using Nedis.Server.Modules.Chat.Domain;

namespace Nedis.Server.Modules.Chat.Infrastructure.Repositories;

public interface IMessagesRepository
{
    Message Add(Message message);
    Task<int> SaveChangesAsync();
}
