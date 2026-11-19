namespace Nedis.Server.Modules.Identity.Features.Logout;

public interface ILogoutService
{
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
}