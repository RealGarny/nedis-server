using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nedis.Server.Common.Web;

namespace Nedis.Server.Modules.Identity.Features.Register;

internal static class RegisterEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterRequest req, IRegisterService registerService) =>
        {
            var login = req.Login.Trim();

            var result = await registerService.RegisterAsync(req);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : ApiProblem.Create(StatusCodes.Status409Conflict, "login_taken", "This login is already taken.");
        });
    }
}
