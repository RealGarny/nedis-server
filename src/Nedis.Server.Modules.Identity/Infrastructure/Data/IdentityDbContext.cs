
using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Identity.Domain;

namespace Nedis.Server.Modules.Identity.Infrastructure.Data;

public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();
    public DbSet<GlobalIdentity> GlobalIdentities => Set<GlobalIdentity>();

    public DbSet<Session> Sessions => Set<Session>();

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

        modelBuilder.Entity<Session>(e =>
        {
            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(s => s.CurrentHash).HasMaxLength(64);
            e.Property(s => s.PreviousHash).HasMaxLength(64);

            e.HasIndex(s => s.ExpiresAt); // daily cleanup deletes by expiry

            // Npgsql maps a uint row version onto PostgreSQL's xmin system column.
            e.Property(s => s.Version).IsRowVersion();
        }
        );
    }
}