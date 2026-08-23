namespace Nedis.Server.Modules.Chat.Domain;

public class Message
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ChannelId { get; set; }
    public Channel Channel = null!;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty; // Temporary
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}