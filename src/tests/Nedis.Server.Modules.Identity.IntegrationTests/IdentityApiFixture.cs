using Microsoft.AspNetCore.TestHost;
using Nedis.Server.Modules.Identity.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Nedis.Server.Modules.Identity.IntegrationTests;

public sealed class IdentityApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("identity_test")
        .WithUsername("user")
        .WithPassword("password")
        .Build();

    private WebApplication? _app;
    public HttpClient? _client;
    private bool _started = false;

    public HttpClient Client => _client
        ?? throw new InvalidOperationException("The integration test fixture has not been initialized.");

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "identity-tests",
                ["Jwt:Audience"] = "identity-tests",
                ["Jwt:SigningKey"] = "integration-test-signing-key-32-bytes-min!!",
                ["RefreshTokens:SigningKey"] = "integration-test-refresh-key-at-least-32-bytes",
                ["RefreshTokens:LifetimeDays"] = "30",
                ["RefreshTokens:GraceWindowSeconds"] = "10",
            })
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer(); // подменяет транспорт на in-memory, конвейер настоящий

        builder.Services.AddIdentityModule(config, _postgres.GetConnectionString());

        _app = builder.Build();
        _app.MapIdentityModule();

        await _app.StartAsync();
        _started = true;

        // Tip: for this to work you should create a migration for your latest dbContext
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await db.Database.MigrateAsync();
        }

        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            try
            {
                if (_started)
                    await _app.StopAsync();
            }
            finally
            {
                await _app.DisposeAsync();
            }
        }
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition(nameof(IdentityApiCollection))]
public class IdentityApiCollection : ICollectionFixture<IdentityApiFixture>;
