using Microsoft.AspNetCore.TestHost;
using Nedis.Server.Modules.Identity.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Nedis.Server.Modules.Identity.IntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("identity_test")
        .WithUsername("chat")
        .WithPassword("chat_test_password")
        .Build();

    private WebApplication _app = null!;

    public HttpClient Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "identity-tests",
                ["Jwt:Audience"] = "identity-tests",
                ["Jwt:SigningKey"] = "integration-test-signing-key-32-bytes-min!!",
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer(); // подменяет транспорт на in-memory, конвейер настоящий

        builder.Services.AddIdentityModule(config, _postgres.GetConnectionString());

        _app = builder.Build();
        _app.MapIdentityModule();

        await _app.StartAsync();

        // Tip: for this to work you should create a migration for your latest dbContext
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Database.MigrateAsync();
        }

        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(nameof(IdentityApiCollection))]
public class IdentityApiCollection : ICollectionFixture<IdentityApiFixture>;
