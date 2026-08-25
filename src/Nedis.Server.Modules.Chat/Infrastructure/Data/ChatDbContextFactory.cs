using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nedis.Server.Modules.Chat.Infrastructure.Data;

public class ChatDbContextFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException(
                "Migration failed. ConnectionStrings__Default is not defined. Before dotnet ef, set an environment variable, " +
                "for example (PowerShell): $env:ConnectionStrings__Default = \"Host=localhost;Port=5432;Database=chatdb;Username=...;Password=...\""
            );

        var optionsBuilder = new DbContextOptionsBuilder<ChatDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ChatDbContext(optionsBuilder.Options);
    }
}
