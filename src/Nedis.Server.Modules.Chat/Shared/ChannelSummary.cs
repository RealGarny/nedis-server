namespace Nedis.Server.Modules.Chat.Shared;

public record ChannelSummary(Guid Id, string Name, DateTimeOffset CreatedAt);