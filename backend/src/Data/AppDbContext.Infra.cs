using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureInfra(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProvider>(entity =>
        {
            entity.ToTable("user_providers");
            entity.HasKey(u => new { u.Provider, u.ProviderId });
            entity.Property(u => u.Provider).IsRequired().HasMaxLength(50);
            entity.Property(u => u.ProviderId).IsRequired().HasMaxLength(300);
            entity.Property(u => u.AccessToken).HasColumnType("text");
            entity.Property(u => u.RefreshToken).HasColumnType("text");
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();

            entity.HasOne(u => u.User)
                  .WithMany(u => u.UserProviders)
                  .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CvTemplate>(entity =>
        {
            entity.ToTable("cv_templates");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).ValueGeneratedOnAdd();
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasColumnType("text");
            entity.Property(t => t.HtmlTemplate).IsRequired().HasColumnType("text");
            entity.Property(t => t.Css).HasColumnType("text");
            entity.Property(t => t.IsActive).HasDefaultValue(false);
            entity.Property(t => t.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(t => t.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<JobProvider>(entity =>
        {
            entity.ToTable("job_providers");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.Portal).IsRequired().HasMaxLength(50);
            entity.Property(p => p.ProviderName).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => new { p.Portal, p.ProviderName }).IsUnique();
            entity.Property(p => p.IsActive).HasDefaultValue(true);
            entity.Property(p => p.IsEnabled).HasDefaultValue(false);
            entity.Property(p => p.BaseUrl).HasMaxLength(300);
            entity.Property(p => p.ApiKey).HasMaxLength(500);
            entity.Property(p => p.Config).HasColumnType("jsonb");
            entity.Property(p => p.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(p => p.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });

        modelBuilder.Entity<AdminLog>(entity =>
        {
            entity.ToTable("admin_logs");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).ValueGeneratedOnAdd();
            entity.Property(l => l.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(l => l.Actor).IsRequired().HasMaxLength(200);
            entity.Property(l => l.Action).IsRequired().HasMaxLength(200);
            entity.Property(l => l.Payload1).HasColumnType("jsonb");
            entity.Property(l => l.Payload2).HasColumnType("text");
            entity.Property(l => l.Payload3).HasColumnType("text");
            entity.Property(l => l.CorrelationId).IsRequired().HasMaxLength(50);

            entity.HasIndex(l => l.CreatedAt).IsDescending();
            entity.HasIndex(l => l.Actor);
            entity.HasIndex(l => l.Action);
            entity.HasIndex(l => l.CorrelationId);
        });

        modelBuilder.Entity<BlockedKeyword>(entity =>
        {
            entity.ToTable("blocked_keywords");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).ValueGeneratedOnAdd();
            entity.Property(b => b.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(b => b.Name).IsUnique();
            entity.Property(b => b.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();

            entity.HasOne(b => b.RedirectKeyword)
                  .WithMany()
                  .HasForeignKey(b => b.RedirectTo)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(b => b.RedirectTo)
                  .HasDatabaseName("idx_blocked_keywords_redirect");
        });
    }
}
