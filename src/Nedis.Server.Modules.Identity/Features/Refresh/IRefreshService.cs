using Nedis.Server.Common;
using Nedis.Server.Modules.Identity.Shared;

namespace Nedis.Server.Modules.Identity.Features.Refresh;

public interface IRefreshService
{
    Task<ServiceResult<AuthResponse, RefreshError>> RefreshAsync(RefreshRequest request, CancellationToken ct);
}