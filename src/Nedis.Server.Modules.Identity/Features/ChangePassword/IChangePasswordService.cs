using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.ChangePassword;

public interface IChangePasswordService
{
    Task<ServiceResult<AuthResponse, ChangePasswordError>> ChangeAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct);
}