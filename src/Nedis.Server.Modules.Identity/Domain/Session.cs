
namespace Nedis.Server.Modules.Identity.Domain;

// One sign-in: a browser profile or app install that logged in. Every refresh
// token of that sign-in is described by this single row, however many times
// it has been rotated; old tokens are recognized by their generation number,
// which the token carries itself (see RefreshTokenCodec).
//
// Several sessions per user are normal: phone, laptop, a second browser.
// Each needs its own chain, because a chain may have only one holder: two
// devices sharing one would look exactly like a stolen token.
public class Session
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    // Updated on every exchange. Groundwork for a "devices" list.
    public DateTimeOffset LastUsedAt { get; set; }

    // Sliding: moved forward on every exchange.
    public DateTimeOffset ExpiresAt { get; set; }

    // Logout, password change or detected theft. Revoked sessions never come back.
    public DateTimeOffset? RevokedAt { get; set; }

    // Generation of the current token; starts at 1, grows by one per exchange.
    public int Generation { get; set; } = 0;


    // SHA-256 of the secret part of the current token (generation = Generation).
    // Only hashes are stored: a database dump must not yield working tokens.
    public string CurrentHash { get; set; } = string.Empty;

    // Hash of the token of generation Generation - 1, the one exchanged for the
    // current token. Kept for the grace window and for crash recovery.
    public string? PreviousHash { get; set; }

    // When the previous token was exchanged: start of its grace window.
    public DateTimeOffset? RotatedAt { get; set; }

    // Optimistic concurrency token. Npgsql maps a uint row version onto
    // PostgreSQL's xmin system column, so no real column is added. Two parallel
    // exchanges of the same token both modify this row; the second one fails
    // with DbUpdateConcurrencyException instead of rotating twice.
    public uint Version { get; set; }
}