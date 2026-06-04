using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Id).ValueGeneratedOnAdd();
            entity.Property(j => j.ExternalId).IsRequired().HasMaxLength(100);
            entity.Property(j => j.Source).IsRequired().HasMaxLength(50).HasDefaultValue("linkedin");
            entity.HasIndex(j => new { j.ExternalId, j.Source }).IsUnique();
            entity.Property(j => j.Title).HasMaxLength(500);
            entity.Property(j => j.Location).HasMaxLength(500);
            entity.Property(j => j.Salary).HasMaxLength(200);
            entity.Property(j => j.Benefits).HasColumnType("text");
            entity.Property(j => j.JobType).HasMaxLength(200);
            entity.Property(j => j.ExperienceLevel).HasMaxLength(200);
            entity.Property(j => j.Industry).HasMaxLength(200);
            entity.Property(j => j.JobFunction).HasMaxLength(200);
            entity.Property(j => j.Applicants).HasMaxLength(100);
            entity.Property(j => j.Description).HasColumnType("text");
            entity.Property(j => j.JobUrl).HasColumnType("text");
            entity.Property(j => j.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(j => j.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();

            entity.HasOne(j => j.Company)
                  .WithMany(c => c.Jobs)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(j => j.CompanyId);
            entity.HasIndex(j => j.PostedDate);
        });

        modelBuilder.Entity<Job>()
            .HasMany(j => j.Categories)
            .WithMany(c => c.Jobs)
            .UsingEntity<Dictionary<string, object>>(
                "job_categories",
                j => j.HasOne<Category>().WithMany().HasForeignKey("category_id")
                      .OnDelete(DeleteBehavior.Cascade),
                c => c.HasOne<Job>().WithMany().HasForeignKey("job_id")
                      .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("job_id", "category_id");
                    join.HasIndex("category_id");
                    join.ToTable("job_categories");
                });

        modelBuilder.Entity<Job>()
            .HasMany(j => j.Keywords)
            .WithMany(k => k.Jobs)
            .UsingEntity<Dictionary<string, object>>(
                "job_keywords",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("keyword_id")
                      .OnDelete(DeleteBehavior.Cascade),
                k => k.HasOne<Job>().WithMany().HasForeignKey("job_id")
                      .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("job_id", "keyword_id");
                    join.HasIndex("keyword_id").HasDatabaseName("idx_job_keywords_keyword");
                    join.HasIndex("job_id").HasDatabaseName("idx_job_keywords_job");
                });
    }
}
