
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Identity.Domain.Entities;

namespace Nedis.Server.Modules.Identity.Data;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();
    public DbSet<GlobalIdentity> GlobalIdentities => Set<GlobalIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Login).HasMaxLength(32);
            e.HasIndex(u => u.Login).IsUnique();
        });

        modelBuilder.Entity<LocalCredential>(e =>
        {
            e.HasKey(c => c.UserId);
            e.HasOne(c => c.User)
                .WithOne(u => u.LocalCredential)
                .HasForeignKey<LocalCredential>(c => c.UserId);
        });

        modelBuilder.Entity<GlobalIdentity>(e =>
        {
            e.HasKey(g => g.UserId);
            e.HasOne(g => g.User)
                .WithOne(u => u.GlobalIdentity)
                .HasForeignKey<GlobalIdentity>(g => g.UserId);
        });
    }
}