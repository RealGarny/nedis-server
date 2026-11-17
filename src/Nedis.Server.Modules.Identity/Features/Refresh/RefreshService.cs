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
            return Failure(RefreshError.InvalidToken);

        var session = await sessions.GetWithUserAsync(presented.SessionId, ct);
        var now = time.GetUtcNow();

        if (session is null || session.RevokedAt is not null || session.ExpiresAt <= now)
            return Failure(RefreshError.InvalidToken);

        AuthResponse response;

        if (presented.Generation == session.Generation && RefreshTokenCodec.HashesEqual(presented.SecretHash, session.CurrentHash))
        {
            response = issuer.Rotate(session);
        }
        else if (presented.Generation == session.Generation - 1 &&
                session.PreviousHash is not null &&
                RefreshTokenCodec.HashesEqual(presented.SecretHash, session.PreviousHash))
        {
            if (session.RotatedAt is { } rotatedAt && now - rotatedAt <= GraceWindow)
            {
                return graceCache.TryGetValue(GraceKey(presented.SecretHash), out AuthResponse? cached) && cached is not null
                    ? Success(cached)
                    : Failure(RefreshError.ExchangeInProgress);
            }
            response = issuer.Reissue(session);
        }
        else
        {
            LogRefreshTokenReuse(logger, session.Id, session.UserId);
            return Failure(RefreshError.InvalidToken);
        }

        try
        {
            await sessions.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Failure(RefreshError.ExchangeInProgress);
        }

        graceCache.Set(GraceKey(presented.SecretHash), response, GraceWindow);
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