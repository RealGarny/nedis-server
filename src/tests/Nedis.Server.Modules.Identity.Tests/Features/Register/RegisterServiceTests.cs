using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Register;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;
using Nedis.Server.Modules.Identity.Shared;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Nedis.Server.Modules.Identity.Tests.Features.Register;

public class RegisterServiceTests
{
    private readonly IUsersRepository _repository = Substitute.For<IUsersRepository>();
    private readonly ISessionIssuer _issuer = Substitute.For<ISessionIssuer>();

    private readonly IPasswordHasher<User> _hasher = new PasswordHasher<User>();

    private RegisterService CreateSut()
    {
        _issuer.StartSession(Arg.Any<User>()).Returns(call =>
        {
            var user = call.Arg<User>();
            return new AuthResponse("fake-jwt-token", "fake-refresh-token", 900, user.Id, user.Login);
        });
        return new(_repository, _issuer, _hasher);
    }

    [Fact]
    public async Task RegisterAsync_NewLogin_ReturnsSuccessWithToken()
    {
        _repository.Add(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());

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

        await CreateSut().RegisterAsync(new RegisterRequest("bob", "correct horse battery staple"));

        Assert.NotNull(savedUser?.LocalCredential);
        Assert.NotEqual("correct horse battery staple", savedUser!.LocalCredential!.PasswordHash);

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
