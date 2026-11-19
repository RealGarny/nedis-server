using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Nedis.Server.Modules.Identity.Infrastructure.Tokens;

internal readonly record struct RefreshTokenParts(Guid SessionId, int Generation, string SecretHash);

// Refresh token format: {sessionId}.{generation}.{secret}.{mac}
//
// - sessionId and generation let the server find the session row and tell
//   the current token from an old one without storing every token ever issued;
// - the MAC makes them unforgeable. Without it anyone who learned a session id
//   could craft a token with an old generation and get that session revoked;
// - the secret proves possession. Its hash is compared with the stored one,
//   so a leaked signing key alone is not enough to use someone's session.
//
// Clients treat the whole string as opaque.
class RefreshTokenCodec(IOptions<RefreshTokenOptions> options)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.SigningKey);

    public string Encode(Guid sessionId, int generation, out string secretHash)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(generation);

        var secret = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var body = string.Create(CultureInfo.InvariantCulture, $"{sessionId:N}.{generation}.{secret}");

        secretHash = HashSecret(secret);
        return $"{body}.{Base64Url.EncodeToString(Sign(body))}";
    }

    // False for anything we did not issue: garbage, truncated or tampered tokens.
    public bool TryDecode(string? raw, out RefreshTokenParts parts)
    {
        parts = default;

        // Reject oversized input before allocating or computing the MAC.
        if (string.IsNullOrEmpty(raw) || raw.Length > 131)
            return false;

        var lastDot = raw.LastIndexOf('.');
        if (lastDot <= 0)
            return false;

        var body = raw[..lastDot];

        if (!MacMatches(body, raw.AsSpan(lastDot + 1)))
            return false;

        ReadOnlySpan<char> bodySpan = body.AsSpan();

        int firstDotIdx = bodySpan.IndexOf('.');
        if (firstDotIdx <= 0) return false;

        int secondsDotIdx = bodySpan[(firstDotIdx + 1)..].IndexOf('.');
        if (secondsDotIdx <= 0) return false;
        secondsDotIdx += firstDotIdx + 1;

        var sessionSpan = bodySpan[..firstDotIdx];
        var generationSpan = bodySpan[(firstDotIdx + 1)..secondsDotIdx];
        var secretSpan = bodySpan[(secondsDotIdx + 1)..];

        if (secretSpan.Length != 43
            || !Guid.TryParseExact(sessionSpan, "N", out var sessionId)
            || !int.TryParse(generationSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var generation)
            || generation <= 0
        )
            return false;

        parts = new RefreshTokenParts(sessionId, generation, HashSecret(secretSpan.ToString()));
        return true;
    }

    public static bool HashesEqual(string presented, string stored) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(presented), Encoding.ASCII.GetBytes(stored));

    private static string HashSecret(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    private byte[] Sign(string body) => HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(body));

    private bool MacMatches(string body, ReadOnlySpan<char> mac)
    {
        var expected = Sign(body);
        var actual = new byte[expected.Length];

        return Base64Url.TryDecodeFromChars(mac, actual, out var written)
            && written == expected.Length
            && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}