using API.Core.Entities;
using API.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<ConnectionCredential> ConnectionCredentials => Set<ConnectionCredential>();
    public DbSet<ConnectionMapping> ConnectionMappings => Set<ConnectionMapping>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<SyncRunRecord> SyncRunRecords => Set<SyncRunRecord>();
    public DbSet<ReferenceData> ReferenceData => Set<ReferenceData>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<NotificationConfig> NotificationConfigs => Set<NotificationConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------
        // Global query filters for soft delete
        // ---------------------------------------------------------------
        modelBuilder.Entity<UserProfile>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Connection>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ConnectionCredential>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ConnectionMapping>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncRun>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncRunRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ReferenceData>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<NotificationConfig>().HasQueryFilter(e => !e.IsDeleted);

        // ---------------------------------------------------------------
        // UserProfile
        // ---------------------------------------------------------------
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AzureAdId).HasMaxLength(128);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => e.AzureAdId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // ---------------------------------------------------------------
        // Connection
        // ---------------------------------------------------------------
        modelBuilder.Entity<Connection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.BaseUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.AuthType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ScheduleCron).HasMaxLength(128);
            entity.Property(e => e.ClientName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PlatformName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.SftpHost).HasMaxLength(512);
            entity.Property(e => e.SftpPath).HasMaxLength(1024);
            entity.Property(e => e.EndpointPath).HasMaxLength(2048);
            entity.Property(e => e.PaginationStrategy).HasMaxLength(128);

            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ClientName);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(u => u.CreatedConnections)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------
        // ConnectionCredential
        // ---------------------------------------------------------------
        modelBuilder.Entity<ConnectionCredential>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CredentialType).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.KeyVaultSecretName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(128);

            entity.HasIndex(e => e.ConnectionId);

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.Credentials)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------
        // ConnectionMapping
        // ---------------------------------------------------------------
        modelBuilder.Entity<ConnectionMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourcePath).HasMaxLength(1024).IsRequired();
            entity.Property(e => e.TargetColumn).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TransformType).HasConversion<string>().HasMaxLength(50);

            entity.HasIndex(e => e.ConnectionId);

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.Mappings)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------
        // SyncRun
        // ---------------------------------------------------------------
        modelBuilder.Entity<SyncRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.FileName).HasMaxLength(512);
            entity.Property(e => e.TriggeredBy).HasMaxLength(256).IsRequired();
            entity.Property(e => e.BlobStorageUrl).HasMaxLength(2048);

            entity.HasIndex(e => e.ConnectionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Connection)
                .WithMany(c => c.SyncRuns)
                .HasForeignKey(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------
        // SyncRunRecord
        // ---------------------------------------------------------------
        modelBuilder.Entity<SyncRunRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetId).HasMaxLength(256);
            entity.Property(e => e.AssetName).HasMaxLength(512);
            entity.Property(e => e.SubmeterCode).HasMaxLength(256);
            entity.Property(e => e.UtilityType).HasMaxLength(128);
            entity.Property(e => e.Value).HasPrecision(18, 6);

            entity.HasIndex(e => e.SyncRunId);
            entity.HasIndex(e => e.IsValid);

            entity.HasOne(e => e.SyncRun)
                .WithMany(s => s.Records)
                .HasForeignKey(e => e.SyncRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------------------------------------------------------------
        // ReferenceData
        // ---------------------------------------------------------------
        modelBuilder.Entity<ReferenceData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AssetId).HasMaxLength(256).IsRequired();
            entity.Property(e => e.AssetName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.SubmeterCode).HasMaxLength(256).IsRequired();
            entity.Property(e => e.UtilityType).HasMaxLength(128).IsRequired();

            entity.HasIndex(e => e.AssetId);
            entity.HasIndex(e => e.SubmeterCode);
            entity.HasIndex(e => e.UploadedById);

            entity.HasOne(e => e.UploadedBy)
                .WithMany()
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------------------------------------------------------------
        // AuditLog (immutable — no soft delete, no cascade)
        // ---------------------------------------------------------------
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(256).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(256).IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ---------------------------------------------------------------
        // NotificationConfig (one-to-one with Connection)
        // ---------------------------------------------------------------
        modelBuilder.Entity<NotificationConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmailRecipients).HasMaxLength(2048);
            entity.Property(e => e.WebhookUrl).HasMaxLength(2048);

            entity.HasIndex(e => e.ConnectionId).IsUnique();

            entity.HasOne(e => e.Connection)
                .WithOne(c => c.NotificationConfig)
                .HasForeignKey<NotificationConfig>(e => e.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
