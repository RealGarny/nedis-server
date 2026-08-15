using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nedis.Server.Modules.Identity.Features.Register;

internal static class RegisterEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterRequest req, IRegisterService registerService) =>
        {
            var login = req.Login.Trim();

            //CRITICAL: implement an adequate field checker and a Response error class.
            if (login.Length is < 3 or > 32)
                return Results.BadRequest(new { error = "The Login must be between 3 and 32 characters long." });
            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "The Password must be at least 8 characters long" });

            var result = await registerService.RegisterAsync(req);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = "Login is already taken" });
        });
    }
}
