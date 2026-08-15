using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nedis.Server.Modules.Identity.Infrastructure.Data;

namespace Nedis.Server.Modules.Identity.Infrastructure.Data;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=nedisdb;Username=nedisdbuser;Password=nedisdbuserpassword";

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new IdentityDbContext(optionsBuilder.Options);
    }
}