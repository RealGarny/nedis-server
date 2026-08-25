namespace Nedis.Server.Modules.Chat.Features.SendMessage;

public record SendMessageRequest(Guid ChannelId, Guid AuthorId, string AuthorName, string Content);