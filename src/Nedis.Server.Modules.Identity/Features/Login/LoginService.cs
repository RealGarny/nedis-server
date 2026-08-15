using Microsoft.AspNetCore.Identity;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Login;

internal class LoginService(
    IUsersRepository userRepository,
    IUserTokenService tokenService,
    IPasswordHasher<User> hasher
    ) : ILoginService
{
    private static readonly string DummyPasswordHash =
            new PasswordHasher<User>().HashPassword(new User(), "dummy-password-for-timing-safety");
    public async Task<ServiceResult<AuthResponse, LoginError>> LoginAsync(LoginRequest loginDto, CancellationToken ct)
    {
        User? user = await userRepository.GetByLoginAsync(loginDto.Login, ct);

        //check if user exists
        if (user?.LocalCredential is null)
        {
            hasher.VerifyHashedPassword(new User(), DummyPasswordHash, loginDto.Password); // fake operation to hide server's timings.
            return ServiceResult<AuthResponse, LoginError>.Failure(LoginError.InvalidCredentials);
        }

        //check user's password hash
        var result = hasher.VerifyHashedPassword(user, user.LocalCredential.PasswordHash, loginDto.Password);

        if (result == PasswordVerificationResult.Failed)
            return ServiceResult<AuthResponse, LoginError>.Failure(LoginError.InvalidCredentials);

        //udate password hash if needed
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.LocalCredential.PasswordHash = hasher.HashPassword(user, loginDto.Password);
        }
        await userRepository.SaveChangesAsync();
        //return credentials
        return ServiceResult<AuthResponse, LoginError>.Success(new AuthResponse(tokenService.Issue(user), user.Id, user.Login));
    }
}
