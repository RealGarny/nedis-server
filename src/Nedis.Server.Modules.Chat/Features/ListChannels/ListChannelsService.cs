using Nedis.Server.Modules.Chat.Infrastructure.Repositories;
using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.ListChannels;

internal class ListChannelsService(IChannelsRepository repository) : IListChannelsService
{
    public async Task<List<ChannelSummary>> ListAsync()
    {
        var channels = await repository.GetAllAsync();
        return [.. channels.Select(c => new ChannelSummary(c.Id, c.Name, c.CreatedAt))];
    }
};