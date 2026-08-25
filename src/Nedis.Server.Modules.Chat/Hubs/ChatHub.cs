using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nedis.Server.Modules.Chat.Features.SendMessage;

namespace Nedis.Server.Modules.Chat.Hubs;

[Authorize]
public class ChatHub(ISendMessageService sendMessageService) : Hub
{
    public async Task SendMessage(Guid channelId, string text)
    {
        text = text.Trim();

        if (text.Length is 0 or > 2000)
            throw new HubException("The message must be between 1 and 2000 characters long");

        var authorId = Guid.Parse(Context.UserIdentifier!);
        var authorNickname = Context.User?.Identity?.Name ?? "?";

        var result = await sendMessageService.SendAsync(
            new SendMessageRequest(channelId, authorId, authorNickname, text));

        if (!result.IsSuccess)
            throw result.Error switch
            {
                SendMessageError.ChannelNotFound => new HubException("Channel not found"),
                _ => new HubException("Unknown Error"),
            };

        await Clients.All.SendAsync("MessagePosted", result.Value);
    }
}