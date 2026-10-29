using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Register;

internal class RegisterService(
    IUsersRepository userRepository,
    ISessionIssuer sessions,
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

        userRepository.Add(user);

        AuthResponse response = sessions.StartSession(user);

        try
        {
            await userRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult<AuthResponse, RegisterError>.Failure(RegisterError.UserAlreadyExists);
        }


        await userRepository.SaveChangesAsync();

        return ServiceResult<AuthResponse, RegisterError>.Success(response);
    }
}
