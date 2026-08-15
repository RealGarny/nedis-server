using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Register;

internal class RegisterService(
    IUsersRepository userRepository,
    IUserTokenService tokenService,
    IPasswordHasher<User> hasher
    ) : IRegisterService
{
    //Possible change: localCredentials creation needs to be injected inside this class.
    public async Task<ServiceResult<AuthResponse, RegisterError>> RegisterAsync(RegisterRequest request)
    {
        var user = new User
        {
            Login = request.Login,
        };
        user.LocalCredential = new LocalCredential
        {
            User = user,
            PasswordHash = hasher.HashPassword(user, request.Password),
        };

        var createdUser = userRepository.Add(user);
        try
        {
            await userRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<AuthResponse, RegisterError>.Failure(RegisterError.UserAlreadyExists);
        }

        return ServiceResult<AuthResponse, RegisterError>.Success(
            new AuthResponse(tokenService.Issue(createdUser), createdUser.Id, createdUser.Login));
    }
}
