using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nedis.Server.Modules.Chat.Features.ListChannels;

internal static class ListChannelsEndpoint
{
    public static void Map(IEndpointRouteBuilder group)
    {
        group.MapGet("/channels", async (IListChannelsService service) =>
            Results.Ok(await service.ListAsync()))
        .RequireAuthorization();
    }
}