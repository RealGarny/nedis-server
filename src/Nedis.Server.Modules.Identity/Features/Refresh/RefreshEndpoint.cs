using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nedis.Server.Common.Web;
using Nedis.Server.Modules.Identity.Features.Refresh;

internal static class RefreshEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/refresh", async (RefreshRequest req, IRefreshService refreshService, CancellationToken ct) =>
        {
            var result = await refreshService.RefreshAsync(req, ct);

            if (result.IsSuccess) return Results.Ok(result.Value);

            return result.Error switch
            {
                RefreshError.InvalidToken => ApiProblem.Create(StatusCodes.Status401Unauthorized, "invalid_refresh_token", "The refresh token is invalid."),
                RefreshError.ExchangeInProgress => ApiProblem.Create(StatusCodes.Status409Conflict, "refresh_in_progress", "The token is being exchanged. Retry shortly."),
                _ => ApiProblem.Create(StatusCodes.Status500InternalServerError, "unexpected", "A server error occured during operation."),
            };
        });
    }
}