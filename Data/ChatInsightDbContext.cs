using ChatInsight.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace ChatInsight.Api.Data;

public class ChatInsightDbContext : DbContext
{
    public ChatInsightDbContext(DbContextOptions<ChatInsightDbContext> options)
        : base(options) { }

    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ChatInsightRecord> Insights => Set<ChatInsightRecord>();
    public DbSet<PersonalityRecord> Personalities => Set<PersonalityRecord>();
    public DbSet<AiJob> AiJobs => Set<AiJob>();
    public DbSet<LifeTimelineRecord> LifeTimelines => Set<LifeTimelineRecord>();
    public DbSet<PersonalityEvolutionRecord> PersonalityEvolutions => Set<PersonalityEvolutionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(512);
            e.Property(x => x.Type).HasMaxLength(64);
            e.HasIndex(x => x.SourceId);
            e.HasMany(x => x.Messages).WithOne(x => x.Chat)
                .HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.Author).HasMaxLength(256);
            e.Property(x => x.Date).HasColumnType("timestamp without time zone");
            e.HasIndex(x => x.ChatId);
            e.HasIndex(x => new { x.ChatId, x.Date });
        });

        modelBuilder.Entity<ChatInsightRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChatId).IsUnique();
            e.HasOne(x => x.Chat).WithOne()
                .HasForeignKey<ChatInsightRecord>(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Model).HasMaxLength(128);
        });

        modelBuilder.Entity<PersonalityRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ChatId, x.Participant }).IsUnique();
            e.HasOne(x => x.Chat).WithMany()
                .HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Participant).HasMaxLength(256);
            e.Property(x => x.Model).HasMaxLength(128);
        });

        modelBuilder.Entity<AiJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.JobType).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(32);
            e.HasIndex(x => new { x.ChatId, x.JobType, x.Status });
            e.HasOne<Chat>().WithMany()
                .HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LifeTimelineRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChatId).IsUnique();
            e.HasOne(x => x.Chat).WithOne()
                .HasForeignKey<LifeTimelineRecord>(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Model).HasMaxLength(128);
        });

        modelBuilder.Entity<PersonalityEvolutionRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ChatId).IsUnique();
            e.HasOne(x => x.Chat).WithOne()
                .HasForeignKey<PersonalityEvolutionRecord>(x => x.ChatId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Model).HasMaxLength(128);
        });
    }
}
