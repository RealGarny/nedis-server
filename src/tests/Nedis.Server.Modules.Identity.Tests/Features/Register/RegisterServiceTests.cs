using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Register;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Nedis.Server.Modules.Identity.Tests.Features.Register;

public class RegisterServiceTests
{
    private readonly IUsersRepository _repository = Substitute.For<IUsersRepository>();
    private readonly IUserTokenService _tokenService = Substitute.For<IUserTokenService>();

    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();

    private RegisterService CreateSut() => new(_repository, _tokenService, _hasher);

    [Fact]
    public async Task RegisterAsync_NewLogin_ReturnsSuccessWithToken()
    {
        _repository.Add(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());
        _tokenService.Issue(Arg.Any<User>()).Returns("fake-jwt-token");

        var result = await CreateSut().RegisterAsync(new RegisterRequest("alice", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal("fake-jwt-token", result.Value!.AccessToken);
        Assert.Equal("alice", result.Value.Login);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashedPassword_NotPlaintext()
    {
        User? savedUser = null;
        _repository.Add(Arg.Do<User>(u => savedUser = u)).Returns(callInfo => callInfo.Arg<User>());
        _tokenService.Issue(Arg.Any<User>()).Returns("token");

        await CreateSut().RegisterAsync(new RegisterRequest("bob", "correct horse battery staple"));

        Assert.NotNull(savedUser?.LocalCredential);
        Assert.NotEqual("correct horse battery staple", savedUser!.LocalCredential!.PasswordHash);

        // раз хэшер настоящий — можно проверить, что сохранённый хэш реально валиден
        var verify = _hasher.VerifyHashedPassword(savedUser, savedUser.LocalCredential.PasswordHash, "correct horse battery staple");
        Assert.Equal(PasswordVerificationResult.Success, verify);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateLogin_ReturnsUserAlreadyExists()
    {
        _repository.SaveChangesAsync().ThrowsAsync(new DbUpdateException());

        var result = await CreateSut().RegisterAsync(new RegisterRequest("taken", "password123"));

        Assert.False(result.IsSuccess);
        Assert.Equal(RegisterError.UserAlreadyExists, result.Error);
    }

    [Fact]
    public async Task RegisterAsync_UnrelatedDbFailure_PropagatesInsteadOfBeingSwallowed()
    {
        _repository.SaveChangesAsync().ThrowsAsync(new InvalidOperationException("db is on fire"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateSut().RegisterAsync(new RegisterRequest("someone", "password123")));
    }
}
