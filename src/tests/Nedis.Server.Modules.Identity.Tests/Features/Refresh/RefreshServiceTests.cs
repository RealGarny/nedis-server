using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Refresh;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;
using Nedis.Server.Modules.Identity.Shared;
using NSubstitute;

namespace Nedis.Server.Modules.Identity.Tests.Features.Refresh;

public class RefreshServiceTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly ISessionsRepository _sessions = Substitute.For<ISessionsRepository>();
    private readonly IOptions<RefreshTokenOptions> _options = Options.Create(new RefreshTokenOptions
    {
        SigningKey = new string('k', 32),
        GraceWindowSeconds = 10,
        LifetimeDays = 30,
    });
    private readonly RefreshTokenCodec _codec;
    private readonly SessionIssuer _issuer;
    private readonly Session _session;
    private readonly AuthResponse _initial;

    public RefreshServiceTests()
    {
        _codec = new RefreshTokenCodec(_options);
        var accessTokens = Substitute.For<IUserTokenService>();
        accessTokens.Issue(Arg.Any<User>()).Returns("access-token");
        _issuer = new SessionIssuer(_sessions, accessTokens, _codec,
            Options.Create(new JwtOptions()), _options, _time);
        Session? created = null;
        _sessions.When(s => s.Add(Arg.Any<Session>())).Do(call => created = call.Arg<Session>());
        _initial = _issuer.StartSession(new User { Login = "alice" });
        _session = created!;
        _sessions.GetWithUserAsync(_session.Id, Arg.Any<CancellationToken>()).Returns(_session);
        _sessions.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _sessions.When(s => s.RevokeAsync(_session.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()))
            .Do(call => _session.RevokedAt = call.Arg<DateTimeOffset>());
    }

    private RefreshService CreateSut(IMemoryCache? cache = null) =>
        new(_sessions, _issuer, _codec, cache ?? _cache, _options, _time, NullLogger<RefreshService>.Instance);

    private Task<ServiceResult<AuthResponse, RefreshError>> ExchangeAsync(string? token, IMemoryCache? cache = null) =>
        CreateSut(cache).RefreshAsync(new RefreshRequest(token), TestContext.Current.CancellationToken);

    private async Task<AuthResponse> RotateAsync(string token)
    {
        var result = await ExchangeAsync(token);
        Assert.True(result.IsSuccess);
        return result.Value!;
    }

    [Fact]
    public async Task CurrentToken_RotatesAndExtendsSession()
    {
        var oldHash = _session.CurrentHash;
        var oldExpiry = _session.ExpiresAt;
        _time.Advance(TimeSpan.FromHours(1));

        var next = await RotateAsync(_initial.RefreshToken);

        Assert.Equal(2, _session.Generation);
        Assert.Equal(oldHash, _session.PreviousHash);
        Assert.NotEqual(oldHash, _session.CurrentHash);
        Assert.True(_codec.TryDecode(next.RefreshToken, out var parts));
        Assert.Equal(_session.CurrentHash, parts.SecretHash);
        Assert.Equal(2, parts.Generation);
        Assert.Equal(oldExpiry.AddHours(1), _session.ExpiresAt);
    }

    [Fact]
    public async Task PreviousToken_WithinGrace_ReplaysCommittedResponseWithoutSave()
    {
        var next = await RotateAsync(_initial.RefreshToken);
        _time.Advance(TimeSpan.FromSeconds(2));

        var replay = await ExchangeAsync(_initial.RefreshToken);

        Assert.True(replay.IsSuccess);
        Assert.Equal(next.RefreshToken, replay.Value!.RefreshToken);
        Assert.Equal(next.AccessToken, replay.Value.AccessToken);
        Assert.Equal(next.ExpiresIn - 2, replay.Value.ExpiresIn);
        Assert.Equal(2, _session.Generation);
        Assert.Null(_session.RevokedAt);
        await _sessions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviousToken_MissingCacheWithinGrace_ReturnsRetryWithoutMutation()
    {
        await RotateAsync(_initial.RefreshToken);
        using var emptyCache = new MemoryCache(new MemoryCacheOptions());
        var hash = _session.CurrentHash;

        var result = await ExchangeAsync(_initial.RefreshToken, emptyCache);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.ExchangeInProgress, result.Error);
        Assert.Equal(hash, _session.CurrentHash);
        Assert.Null(_session.RevokedAt);
        await _sessions.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviousToken_AfterGrace_ReissuesWithoutExtendingLifetime()
    {
        var first = await RotateAsync(_initial.RefreshToken);
        var expiry = _session.ExpiresAt;
        _time.Advance(TimeSpan.FromSeconds(11));
        using var emptyCache = new MemoryCache(new MemoryCacheOptions());

        var recovered = await ExchangeAsync(_initial.RefreshToken, emptyCache);

        Assert.True(recovered.IsSuccess);
        Assert.NotEqual(first.RefreshToken, recovered.Value!.RefreshToken);
        Assert.Equal(2, _session.Generation);
        Assert.Equal(expiry, _session.ExpiresAt);
        Assert.Equal(TimeSpan.Zero, _session.RotatedAt!.Value.Offset);
        // The original cache still contains the first response. It cannot be used
        // for the replacement because its issued token is no longer current.
        var staleCacheReplay = await ExchangeAsync(_initial.RefreshToken);
        Assert.False(staleCacheReplay.IsSuccess);
        Assert.Equal(RefreshError.ExchangeInProgress, staleCacheReplay.Error);
        var replay = await ExchangeAsync(_initial.RefreshToken, emptyCache);
        Assert.True(replay.IsSuccess);
        Assert.Equal(recovered.Value.RefreshToken, replay.Value!.RefreshToken);
    }

    [Fact]
    public async Task RecoveredToken_WhenUsed_RotatesAndRestoresSlidingLifetime()
    {
        await RotateAsync(_initial.RefreshToken);
        var expiry = _session.ExpiresAt;
        _time.Advance(TimeSpan.FromDays(1));
        var recovered = await RotateAsync(_initial.RefreshToken);
        Assert.Equal(expiry, _session.ExpiresAt);

        await RotateAsync(recovered.RefreshToken);

        Assert.Equal(3, _session.Generation);
        Assert.Equal(expiry.AddDays(1), _session.ExpiresAt);
    }

    [Fact]
    public async Task PreviousToken_RepeatedRecoveryCannotExtendLifetime()
    {
        await RotateAsync(_initial.RefreshToken);
        var expiry = _session.ExpiresAt;
        for (var i = 0; i < 3; i++)
        {
            _time.Advance(TimeSpan.FromDays(1));
            await RotateAsync(_initial.RefreshToken);
            Assert.Equal(expiry, _session.ExpiresAt);
        }
        _time.SetUtcNow(expiry);

        var result = await ExchangeAsync(_initial.RefreshToken);
        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
    }

    [Fact]
    public async Task ReplacedCurrentToken_ReuseRevokesSession()
    {
        var replaced = await RotateAsync(_initial.RefreshToken);
        _time.Advance(TimeSpan.FromSeconds(11));
        await RotateAsync(_initial.RefreshToken);

        var result = await ExchangeAsync(replaced.RefreshToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
        Assert.NotNull(_session.RevokedAt);
    }

    [Fact]
    public async Task OlderGeneration_ReuseRevokesSession()
    {
        var next = await RotateAsync(_initial.RefreshToken);
        await RotateAsync(next.RefreshToken);

        var result = await ExchangeAsync(_initial.RefreshToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
        Assert.NotNull(_session.RevokedAt);
    }

    [Fact]
    public async Task TamperedToken_DoesNotRevokeSession()
    {
        var parts = _initial.RefreshToken.Split('.');
        parts[1] = "2"; // Modify the signed body, not the base64 padding bits.

        var result = await ExchangeAsync(string.Join('.', parts));

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
        Assert.Null(_session.RevokedAt);
        await _sessions.DidNotReceive().GetWithUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignedFutureGeneration_IsRejectedWithoutRevocation()
    {
        var future = _codec.Encode(_session.Id, 10, out _);
        var result = await ExchangeAsync(future);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
        Assert.Null(_session.RevokedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Encode_InvalidGeneration_Throws(int generation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _codec.Encode(Guid.CreateVersion7(), generation, out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Encode_ValidGeneration_RoundTrips(int generation)
    {
        var sessionId = Guid.CreateVersion7();

        var token = _codec.Encode(sessionId, generation, out var hash);

        Assert.True(_codec.TryDecode(token, out var decoded));

        Assert.Equal(sessionId, decoded.SessionId);
        Assert.Equal(generation, decoded.Generation);
        Assert.Equal(hash, decoded.SecretHash);
    }

    [Fact]
    public async Task ConcurrencyConflict_DoesNotPublishUncommittedResponse()
    {
        _sessions.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<int>(new DbUpdateConcurrencyException()));

        var result = await ExchangeAsync(_initial.RefreshToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.ExchangeInProgress, result.Error);
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public async Task RevokedSession_CannotReplayCachedResponse()
    {
        await RotateAsync(_initial.RefreshToken);
        _session.RevokedAt = _time.GetUtcNow();

        var result = await ExchangeAsync(_initial.RefreshToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshError.InvalidToken, result.Error);
    }

    public void Dispose() => _cache.Dispose();
}
