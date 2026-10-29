
using Microsoft.Extensions.Options;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Infrastructure.Tokens;

class SessionIssuer(
    ISessionsRepository sessions,
    IUserTokenService accessTokens,
    RefreshTokenCodec codec,
    IOptions<JwtOptions> jwtOptions,
    IOptions<RefreshTokenOptions> refreshOptions,
    TimeProvider time
    ) : ISessionIssuer
{
    public AuthResponse StartSession(User user)
    {
        var now = time.GetUtcNow();

        var session = new Session
        {
            User = user,
            UserId = user.Id,
            CreatedAt = now,
            Generation = 1,
        };

        sessions.Add(session);

        return Issue(session, now);
    }

    public AuthResponse Rotate(Session session)
    {
        var now = time.GetUtcNow();

        session.PreviousHash = session.CurrentHash;
        session.RotatedAt = now;
        session.Generation++;

        return Issue(session, now);
    }

    public AuthResponse Reissue(Session session)
    {
        var now = time.GetLocalNow();

        session.RotatedAt = now;

        return Issue(session, now);
    }

    private AuthResponse Issue(Session session, DateTimeOffset now)
    {
        var refreshToken = codec.Encode(session.Id, session.Generation, out var secretHash);

        session.CurrentHash = secretHash;
        session.LastUsedAt = now;
        session.ExpiresAt = now.AddDays(refreshOptions.Value.LifetimeDays);

        return new AuthResponse(
            AccessToken: accessTokens.Issue(session.User),
            RefreshToken: refreshToken,
            ExpiresIn: jwtOptions.Value.AccessTokenLifetimeMinutes * 60,
            UserId: session.UserId,
            Login: session.User.Login
        );
    }
}