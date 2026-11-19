using Microsoft.AspNetCore.Identity;
using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.ChangePassword;

internal class ChangePasswordService(
    ISessionsRepository sessions,
    ISessionIssuer issuer,
    IUsersRepository users,
    TimeProvider time,
    IPasswordHasher<User> hasher
) : IChangePasswordService
{
    public async Task<ServiceResult<AuthResponse, ChangePasswordError>> ChangeAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await users.GetWithLocalCredentialAsync(userId, ct);

        if (user is null)
            return Failure(ChangePasswordError.UserNotFound);
        if (user.LocalCredential is null)
            return Failure(ChangePasswordError.NoLocalPassword);
        if (hasher.VerifyHashedPassword(user, user.LocalCredential.PasswordHash, request.OldPassword) == PasswordVerificationResult.Failed)
            return Failure(ChangePasswordError.InvalidCurrentPassword);

        await sessions.RevokeAllAsync(userId, time.GetUtcNow(), ct);

        user.LocalCredential.PasswordHash = hasher.HashPassword(user, request.NewPassword);

        var response = issuer.StartSession(user);

        // One save for the password and the new session: both repositories share
        // the module's scoped DbContext.
        await sessions.SaveChangesAsync(ct);

        return ServiceResult<AuthResponse, ChangePasswordError>.Success(response);
    }

    private static ServiceResult<AuthResponse, ChangePasswordError> Failure(ChangePasswordError error) =>
    ServiceResult<AuthResponse, ChangePasswordError>.Failure(error);
}