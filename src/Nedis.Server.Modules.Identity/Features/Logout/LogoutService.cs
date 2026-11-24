

using Nedis.Server.Modules.Identity.Features.Logout;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;

internal class LogoutService(ISessionsRepository sessions, RefreshTokenCodec codec, TimeProvider time) : ILogoutService
{
    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        if (!codec.TryDecode(request.RefreshToken, out var presented)) return;

        var session = await sessions.GetWithUserAsync(presented.SessionId, ct);

        if (session is null || RefreshTokenCodec.HashesEqual(session.CurrentHash, presented.SecretHash)) return;


        await sessions.RevokeAsync(session.Id, time.GetUtcNow(), ct);
    }
}