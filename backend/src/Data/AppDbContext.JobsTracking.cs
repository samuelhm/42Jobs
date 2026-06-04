using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureJobsTracking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserJob>(entity =>
        {
            entity.ToTable("user_jobs", t => t.HasCheckConstraint(
                "CK_user_jobs_status",
                "status IN ('saved', 'cv_enviado', 'entrevista_conseguida', 'empleo_conseguido', 'rechazado', 'oculto')"));
            entity.HasKey(u => new { u.UserId, u.JobId });
            entity.Property(u => u.SavedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.Notes).HasColumnType("text");
            entity.Property(u => u.Status)
                  .HasMaxLength(30)
                  .HasDefaultValue("saved");
            entity.Property(u => u.StatusUpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();

            entity.HasOne(u => u.User)
                  .WithMany(u => u.UserJobs)
                  .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.Job)
                  .WithMany(j => j.UserJobs)
                  .HasForeignKey(u => u.JobId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(u => u.UserId).HasDatabaseName("idx_user_jobs_user");
            entity.HasIndex(u => u.JobId).HasDatabaseName("idx_user_jobs_job");
        });

        modelBuilder.Entity<UserCategory>(entity =>
        {
            entity.ToTable("user_categories");
            entity.HasKey(uc => new { uc.UserId, uc.CategoryId });
            entity.Property(uc => uc.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();

            entity.HasOne(uc => uc.User)
                  .WithMany(u => u.UserCategories)
                  .HasForeignKey(uc => uc.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uc => uc.Category)
                  .WithMany(c => c.UserCategories)
                  .HasForeignKey(uc => uc.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(uc => uc.UserId).HasDatabaseName("idx_user_categories_user");
            entity.HasIndex(uc => uc.CategoryId).HasDatabaseName("idx_user_categories_category");
        });

        modelBuilder.Entity<UserKeyword>(entity =>
        {
            entity.ToTable("user_keywords", t => t.HasCheckConstraint(
                "CK_user_keywords_learning_status",
                "learning_status IN ('not_learned', 'learned_personal_project', 'learned_in_school')"));
            entity.HasKey(uk => new { uk.UserId, uk.KeywordId });

            entity.Property(uk => uk.LearningStatus)
                  .HasMaxLength(50)
                  .HasDefaultValue("not_learned");

            entity.HasOne(uk => uk.User)
                  .WithMany(u => u.UserKeywords)
                  .HasForeignKey(uk => uk.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(uk => uk.Keyword)
                  .WithMany(k => k.UserKeywords)
                  .HasForeignKey(uk => uk.KeywordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscardedJob>(entity =>
        {
            entity.ToTable("discarded_jobs");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).ValueGeneratedOnAdd();
            entity.Property(d => d.ExternalId).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Source).IsRequired().HasMaxLength(50).HasDefaultValue("linkedin");
            entity.HasIndex(d => new { d.ExternalId, d.Source, d.CategoryName }).IsUnique();
            entity.Property(d => d.Title).HasMaxLength(500);
            entity.Property(d => d.CompanyName).HasMaxLength(500);
            entity.Property(d => d.Location).HasMaxLength(500);
            entity.Property(d => d.Salary).HasMaxLength(200);
            entity.Property(d => d.Benefits).HasColumnType("text");
            entity.Property(d => d.JobUrl).HasColumnType("text");
            entity.Property(d => d.Description).HasColumnType("text");
            entity.Property(d => d.JobType).HasMaxLength(200);
            entity.Property(d => d.ExperienceLevel).HasMaxLength(200);
            entity.Property(d => d.Industry).HasMaxLength(200);
            entity.Property(d => d.JobFunction).HasMaxLength(200);
            entity.Property(d => d.Applicants).HasMaxLength(100);
            entity.Property(d => d.FilterReasons).HasColumnType("text");
            entity.Property(d => d.CategoryName).HasMaxLength(100);
            entity.Property(d => d.RawData).HasColumnType("jsonb");
            entity.Property(d => d.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
        });
    }
}
