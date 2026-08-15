using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nedis.Server.Modules.Identity.Features.Login;

internal static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (LoginRequest req, ILoginService loginService, CancellationToken ct) =>
        {
            var result = await loginService.LoginAsync(req, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        });
    }
}
