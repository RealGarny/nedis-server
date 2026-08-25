namespace Nedis.Server.Modules.Chat.Shared;

public record MessageSummary(
    Guid Id,
    Guid ChannelId,
    Guid AuthorId,
    string AuthorNickname,
    string Content,
    DateTimeOffset SentAt);