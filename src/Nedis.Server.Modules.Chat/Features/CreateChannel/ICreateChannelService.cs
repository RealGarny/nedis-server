using Nedis.Server.Common;
using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.CreateChannel;

interface ICreateChannelService
{
    Task<ServiceResult<ChannelSummary, CreateChannelError>> CreateAsync(CreateChannelRequest request);
}