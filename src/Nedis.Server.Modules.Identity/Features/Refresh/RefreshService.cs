using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Refresh;

internal partial class RefreshService(
    ISessionsRepository sessions,
    ISessionIssuer issuer,
    RefreshTokenCodec codec,
    IMemoryCache graceCache,
    IOptions<RefreshTokenOptions> options,
    TimeProvider time,
    ILogger<RefreshService> logger
    ) : IRefreshService
{
    private TimeSpan GraceWindow => TimeSpan.FromSeconds(options.Value.GraceWindowSeconds);
    public async Task<ServiceResult<AuthResponse, RefreshError>> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        if (!codec.TryDecode(request.RefreshToken, out var presented))
        {
            return Failure(RefreshError.InvalidToken);
        }

        var session = await sessions.GetWithUserAsync(presented.SessionId, ct);
        var now = time.GetUtcNow();

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now)
        {
            return Failure(RefreshError.InvalidToken);
        }

        AuthResponse? response = null;
        int generationDelta = session.Generation - presented.Generation;

        if (generationDelta > 1 || !RefreshTokenCodec.HashesEqual(presented.SecretHash, session.CurrentHash))
        {
            await sessions.RevokeAsync(session.Id, now, ct);
            LogRefreshTokenReuse(logger, session.Id, session.UserId);
            return Failure(RefreshError.InvalidToken);
        }

        switch (generationDelta)
        {
            case 0:
                response = issuer.Rotate(session);
                break;
            case 1:
                if (session.RotatedAt is not { } || now - session.RotatedAt > GraceWindow)
                {
                    LogRefreshTokenReuse(logger, session.Id, session.UserId);
                    return Failure(RefreshError.InvalidToken);
                }

                return graceCache.TryGetValue(GraceKey(presented.SecretHash), out AuthResponse? cached) && cached is not null
                    ? Success(cached)
                    : Failure(RefreshError.ExchangeInProgress);
        }
        try
        {
            await sessions.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(RefreshError.ExchangeInProgress);
        }

        if (response is null)
        {
            return Failure(RefreshError.Unexpected);
        }
        return Success(response);
    }

    private static string GraceKey(string secretHash) => $"refresh-grace:{secretHash}";

    private static ServiceResult<AuthResponse, RefreshError> Success(AuthResponse response) =>
        ServiceResult<AuthResponse, RefreshError>.Success(response);

    private static ServiceResult<AuthResponse, RefreshError> Failure(RefreshError error) =>
        ServiceResult<AuthResponse, RefreshError>.Failure(error);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Refresh token reuse detected: session {SessionId} of user {UserId} revoked")]
    private static partial void LogRefreshTokenReuse(ILogger logger, Guid sessionId, Guid userId);
}