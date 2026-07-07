namespace Nedis.Server.Modules.Identity.Domain.Entities;

public class LocalCredential
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}