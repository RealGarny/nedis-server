using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Register;

public interface IRegisterService
{
    Task<ServiceResult<AuthResponse, RegisterError>> RegisterAsync(RegisterRequest request);
}
