using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using Nedis.Server.Common.Web;
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
                : ApiProblem.Create(StatusCodes.Status401Unauthorized, "invalid_credentials", "Invalid login or password.");
        });
    }
}
