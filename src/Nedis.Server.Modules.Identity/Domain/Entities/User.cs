namespace Nedis.Server.Modules.Identity.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Login { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public LocalCredential? LocalCredential { get; set; }
    public GlobalIdentity? GlobalIdentity { get; set; }
}