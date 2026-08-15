namespace Nedis.Server.Modules.Identity.Domain;

public class GlobalIdentity
{
    public Guid UserId { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
