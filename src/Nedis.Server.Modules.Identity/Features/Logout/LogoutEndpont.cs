using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nedis.Server.Modules.Identity.Features.Logout;

internal static class LogoutEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (LogoutRequest request, ILogoutService logoutService, CancellationToken ct) =>
        {
            await logoutService.LogoutAsync(request, ct);
        });
    }
}