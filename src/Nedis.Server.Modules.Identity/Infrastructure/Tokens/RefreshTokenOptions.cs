namespace Nedis.Server.Modules.Identity.Infrastructure.Tokens;

class RefreshTokenOptions
{
    // Sliding: every exchange extends the session to this long from now,
    // so a session survives as long as it is used at least once per period.
    public int LifetimeDays { get; set; } = 30;

    // How long after an exchange a repeated presentation of the same token
    // gets the already issued pair back instead of being treated as theft.
    public int GraceWindowSeconds { get; set; } = 10;

    // Secret, at least 32 bytes. Signs refresh tokens so the session id and
    // generation inside them cannot be forged. Separate from the JWT key:
    // changing it signs every user out, which should be a deliberate decision.
    public string SignInKey { get; set; } = string.Empty;
}