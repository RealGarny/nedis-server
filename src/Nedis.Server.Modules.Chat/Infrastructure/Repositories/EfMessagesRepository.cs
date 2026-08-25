using Nedis.Server.Modules.Chat.Domain;
using Nedis.Server.Modules.Chat.Infrastructure.Data;

namespace Nedis.Server.Modules.Chat.Infrastructure.Repositories;

internal class EfMessagesRepository(ChatDbContext db) : IMessagesRepository
{
    public Message Add(Message message) => db.Messages.Add(message).Entity;

    public Task<int> SaveChangesAsync() => db.SaveChangesAsync();
}
