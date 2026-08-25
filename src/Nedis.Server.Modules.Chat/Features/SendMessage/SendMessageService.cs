using Nedis.Server.Common;
using Nedis.Server.Modules.Chat.Domain;
using Nedis.Server.Modules.Chat.Infrastructure.Repositories;
using Nedis.Server.Modules.Chat.Shared;

namespace Nedis.Server.Modules.Chat.Features.SendMessage;

internal class SendMessageService(
    IChannelsRepository channelsRepository,
    IMessagesRepository messagesRepository
    ) : ISendMessageService
{
    public async Task<ServiceResult<MessageSummary, SendMessageError>> SendAsync(SendMessageRequest request)
    {
        var channel = await channelsRepository.GetByIdAsync(request.ChannelId);
        if (channel is null)
            return ServiceResult<MessageSummary, SendMessageError>.Failure(SendMessageError.ChannelNotFound);

        var message = new Message
        {
            ChannelId = request.ChannelId,
            AuthorId = request.AuthorId,
            AuthorName = request.AuthorName,
            Content = request.Content,
        };

        messagesRepository.Add(message);
        await messagesRepository.SaveChangesAsync();

        return ServiceResult<MessageSummary, SendMessageError>.Success(new MessageSummary(
            message.Id, message.ChannelId, message.AuthorId, message.AuthorName, message.Content, message.SentAt));
    }
}