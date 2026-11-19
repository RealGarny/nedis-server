using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Login;
using Nedis.Server.Modules.Identity.Features.Refresh;
using Nedis.Server.Modules.Identity.Features.Register;
using Nedis.Server.Modules.Identity.Infrastructure.Background;
using Nedis.Server.Modules.Identity.Infrastructure.Data;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;
using Nedis.Server.Modules.Identity.Infrastructure.Tokens;

namespace Nedis.Server.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config, string connectionString)
    {
        services.AddDbContext<IdentityDbContext>(o =>
            o.UseNpgsql(connectionString, npsql =>
                npsql.MigrationsHistoryTable("__ef_migrations_history", "identity")
            )
        );

        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        //utilites
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        // TryAdd: tests register a FakeTimeProvider before calling AddIdentityModule.
        services.TryAddSingleton(TimeProvider.System);
        services.AddMemoryCache(); // grace window responses; safe to call more than once

        //options
        // Fails at startup.
        services.AddOptions<RefreshTokenOptions>()
            .Bind(config.GetSection("RefreshTokens"))
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "RefreshTokens:SigningKey must be at least 32 bytes."
            )
            .ValidateOnStart();

        //architecture
        services.AddScoped<IUserTokenService, UserTokenService>();
        services.AddScoped<ISessionIssuer, SessionIssuer>();
        services.AddSingleton<RefreshTokenCodec>();

        //repositories
        services.AddScoped<IUsersRepository, EfUsersRepository>();
        services.AddScoped<ISessionsRepository, EfSessionsRepository>();

        //features
        services.AddScoped<IRefreshService, RefreshService>();
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<ILoginService, LoginService>();

        //background services
        services.AddHostedService<SessionsCleanupService>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder app)
    {
        var moduleGroup = app.MapGroup("/api/auth");

        RegisterEndpoint.Map(moduleGroup);
        LoginEndpoint.Map(moduleGroup);
        RefreshEndpoint.Map(moduleGroup);

        return app;
    }
}
