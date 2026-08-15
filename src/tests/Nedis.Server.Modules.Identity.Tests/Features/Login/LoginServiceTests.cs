using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Login;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using NSubstitute;

namespace Nedis.Server.Modules.Identity.Tests.Features.Login;

public class LoginServiceTests
{
    private readonly IUsersRepository _repository = Substitute.For<IUsersRepository>();
    private readonly IUserTokenService _tokenService = Substitute.For<IUserTokenService>();
    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();

    private LoginService CreateSut() => new(_repository, _tokenService, _hasher);

    private User CreateUserWithPassword(string login, string password)
    {
        var user = new User { Id = Guid.NewGuid(), Login = login };
        user.LocalCredential = new LocalCredential
        {
            User = user,
            PasswordHash = _hasher.HashPassword(user, password),
        };
        return user;
    }

    [Fact]
    public async Task LoginAsync_CorrectPassword_ReturnsSuccessWithToken()
    {
        var user = CreateUserWithPassword("alice", "password123");
        _repository.GetByLoginAsync("alice", TestContext.Current.CancellationToken).Returns(user);
        _tokenService.Issue(user).Returns("fake-jwt-token");

        var result = await CreateSut().LoginAsync(new LoginRequest("alice", "password123"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-jwt-token", result.Value!.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsInvalidCredentials()
    {
        var user = CreateUserWithPassword("alice", "password123");
        _repository.GetByLoginAsync("alice", TestContext.Current.CancellationToken).Returns(user);

        var result = await CreateSut().LoginAsync(new LoginRequest("alice", "wrong-password"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LoginAsync_UnknownLogin_ReturnsInvalidCredentials_WithoutThrowing()
    {
        _repository.GetByLoginAsync("ghost", TestContext.Current.CancellationToken).Returns((User?)null);

        var result = await CreateSut().LoginAsync(new LoginRequest("ghost", "whatever12"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task LoginAsync_SuccessRehashNeeded_UpdatesStoredHashAndSaves()
    {
        var ct = TestContext.Current.CancellationToken;
        var weakHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1 }));
        var user = new User { Id = Guid.NewGuid(), Login = "alice" };
        user.LocalCredential = new LocalCredential
        {
            User = user,
            PasswordHash = weakHasher.HashPassword(user, "password123"),
        };
        var oldHash = user.LocalCredential.PasswordHash;

        _repository.GetByLoginAsync("alice", ct).Returns(user);
        _tokenService.Issue(user).Returns("token");

        var result = await CreateSut().LoginAsync(new LoginRequest("alice", "password123"), ct);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(oldHash, user.LocalCredential.PasswordHash);
        await _repository.Received(1).SaveChangesAsync();
    }

    //WARNING: rough estamate. not a real test.
    [Fact]
    [Trait("Category", "Timing")]
    public async Task LoginAsync_UnknownLogin_TakesComparableTimeToWrongPassword()
    {
        var ct = TestContext.Current.CancellationToken;
        var user = CreateUserWithPassword("alice", "password123");
        _repository.GetByLoginAsync("alice", ct).Returns(user);
        _repository.GetByLoginAsync("ghost", ct).Returns((User?)null);
        var sut = CreateSut();

        // прогрев JIT, чтобы не мерить компиляцию метода вместо самой логики
        await sut.LoginAsync(new LoginRequest("alice", "wrong"), ct);
        await sut.LoginAsync(new LoginRequest("ghost", "wrong"), ct);

        var swReal = Stopwatch.StartNew();
        await sut.LoginAsync(new LoginRequest("alice", "wrong-password"), ct);
        swReal.Stop();

        var swFake = Stopwatch.StartNew();
        await sut.LoginAsync(new LoginRequest("ghost", "wrong-password"), ct);
        swFake.Stop();

        var ratio = (double)swFake.ElapsedTicks / swReal.ElapsedTicks;
        Assert.InRange(ratio, 0.3, 3.0);
    }
}
