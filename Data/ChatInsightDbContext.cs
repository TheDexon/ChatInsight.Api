using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Data;

public class ChatInsightDbContext : DbContext
{
    public ChatInsightDbContext(
        DbContextOptions<ChatInsightDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatInsightRecord> Insights => Set<ChatInsightRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512);
            e.Property(x => x.Type).HasMaxLength(64);

            e.HasMany(x => x.Messages)
                .WithOne(x => x.Chat)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Author).HasMaxLength(256);
            e.Property(x => x.Date)
                .HasColumnType("timestamp without time zone");

            e.HasIndex(x => x.ChatId);
            e.HasIndex(x => new { x.ChatId, x.Date });
        });

        modelBuilder.Entity<ChatInsightRecord>(e =>
        {
            e.HasKey(x => x.Id);

            // один инсайт на чат
            e.HasIndex(x => x.ChatId).IsUnique();

            e.HasOne(x => x.Chat)
                .WithOne()
                .HasForeignKey<ChatInsightRecord>(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            // Topics / Dynamics (List<string>) Npgsql маппит в text[] нативно
            e.Property(x => x.Model).HasMaxLength(128);
        });
    }
}
