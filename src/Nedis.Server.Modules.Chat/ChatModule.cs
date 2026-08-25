using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nedis.Server.Modules.Chat.Features.CreateChannel;
using Nedis.Server.Modules.Chat.Features.ListChannels;
using Nedis.Server.Modules.Chat.Hubs;
using Nedis.Server.Modules.Chat.Infrastructure.Data;
using Nedis.Server.Modules.Chat.Infrastructure.Repositories;

namespace Nedis.Server.Modules.Chat;

public static class ChatModule
{
    public static IServiceCollection AddChatModule(this IServiceCollection services, string connectionString)
    {
        services.AddSignalR();
        services.AddDbContext<ChatDbContext>(o =>
            o.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "chat")));

        services.AddScoped<IChannelsRepository, EfChannelsRepository>();

        services.AddScoped<ICreateChannelService, CreateChannelService>();
        services.AddScoped<IListChannelsService, ListChannelsService>();

        return services;
    }

    public static IEndpointRouteBuilder MapChatModule(this IEndpointRouteBuilder app)
    {
        var chatGroup = app.MapGroup("api/chat");

        CreateChannelEndpoint.Map(chatGroup);
        ListChannelsEndpoint.Map(chatGroup);

        app.MapHub<ChatHub>("hub/chat");

        return app;
    }
}