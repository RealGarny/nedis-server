using Microsoft.EntityFrameworkCore;
using Nedis.Server.Modules.Chat.Domain;

namespace Nedis.Server.Modules.Chat.Infrastructure.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("chat");

        modelBuilder.Entity<Channel>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.Property(m => m.Content).HasMaxLength(2000);
            e.Property(m => m.AuthorName).HasMaxLength(32);
            e.HasOne(m => m.Channel)
                .WithMany()
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => new { m.ChannelId, m.SentAt });
        });
    }
}
