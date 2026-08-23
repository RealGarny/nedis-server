using Microsoft.EntityFrameworkCore;
using Nedis.Server.Common;
using Nedis.Server.Modules.Chat.Domain;
using Nedis.Server.Modules.Chat.Infrastructure.Repositories;
using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.CreateChannel;

internal class CreateChannelService(IChannelsRepository repository) : ICreateChannelService
{
    public async Task<ServiceResult<ChannelSummary, CreateChannelError>> CreateAsync(CreateChannelRequest request)
    {
        var channel = new Channel { Name = request.Name };
        var created = repository.Add(channel);
        try
        {
            await repository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<ChannelSummary, CreateChannelError>.Failure(CreateChannelError.UnknownError);
        }
        return ServiceResult<ChannelSummary, CreateChannelError>.Success(
            new ChannelSummary(created.Id, created.Name, created.CreatedAt));
    }
};