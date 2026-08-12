using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nedis.Server.Modules.Identity.Domain;
using Nedis.Server.Modules.Identity.Features.Login;
using Nedis.Server.Modules.Identity.Features.Register;
using Nedis.Server.Modules.Identity.Infrastructure.Data;
using Nedis.Server.Modules.Identity.Infrastructure.Jwt;
using Nedis.Server.Modules.Identity.Infrastructure.Repositories;

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

        //repositories
        services.AddScoped<IUsersRepository, EfUsersRepository>();

        //jwt
        services.AddScoped<IUserTokenService, UserTokenService>();

        //features
        services.AddScoped<IRegisterService, RegisterService>();
        services.AddScoped<ILoginService, LoginService>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder app)
    {
        var moduleGroup = app.MapGroup("/api/auth");

        RegisterEndpoint.Map(moduleGroup);
        LoginEndpoint.Map(moduleGroup);

        return app;
    }
}
