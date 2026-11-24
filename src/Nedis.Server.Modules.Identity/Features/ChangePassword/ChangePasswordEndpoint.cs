using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nedis.Server.Common.Web;

namespace Nedis.Server.Modules.Identity.Features.ChangePassword;

internal static class ChangePasswordEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal principal,
            IChangePasswordService service,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await service.ChangeAsync(userId, request, ct);

            if (result.IsSuccess)
                return Results.Ok(result.Value);

            return result.Error switch
            {
                ChangePasswordError.UserNotFound => ApiProblem.Create(StatusCodes.Status401Unauthorized, "user_not_found", "The account no longer exists."),
                ChangePasswordError.NoLocalPassword => ApiProblem.Create(StatusCodes.Status400BadRequest, "no_local_password", "This account has no local password."),
                _ => ApiProblem.Create(StatusCodes.Status400BadRequest, "invalid_current_password", "The current password is wrong.")
            };
        })
        .RequireAuthorization();
    }
}