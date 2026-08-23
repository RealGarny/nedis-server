
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nedis.Server.Modules.Chat.Features.CreateChannel;

internal static class CreateChannelEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapPost("/channels", async (CreateChannelRequest req, ICreateChannelService service) =>
        {
            var result = await service.CreateAsync(req);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });
    }
}