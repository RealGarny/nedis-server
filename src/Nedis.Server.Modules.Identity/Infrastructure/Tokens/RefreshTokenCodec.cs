using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Nedis.Server.Modules.Identity.Infrastructure.Tokens;

internal readonly record struct RefreshTokenParts(Guid SessionId, int Generation, string SecretHash);

class RefreshTokenCodec(IOptions<RefreshTokenOptions> options)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.Value.SignInKey);

    public string Encode(Guid sessionId, int generation, out string secretHash)
    {
        var secret = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var body = string.Create(CultureInfo.InvariantCulture, $"{sessionId:N}.{generation}.{secret}");

        secretHash = HashSecret(secret);

        return $"{body}.{Base64Url.EncodeToString(Sign(body))}";
    }

    public bool TryDecode(string? raw, out RefreshTokenParts parts)
    {
        parts = default;

        if (string.IsNullOrEmpty(raw))
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

        if (secretSpan.IsEmpty
            || !Guid.TryParseExact(sessionSpan, "N", out var sessionId)
            || !int.TryParse(generationSpan, out var generation)
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