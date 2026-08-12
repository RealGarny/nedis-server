using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Login;

public interface ILoginService
{
    Task<ServiceResult<AuthResponse, LoginError>> LoginAsync(LoginRequest request);
}
