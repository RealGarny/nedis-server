using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.ListChannels;

public interface IListChannelsService
{
    Task<List<ChannelSummary>> ListAsync();
}
