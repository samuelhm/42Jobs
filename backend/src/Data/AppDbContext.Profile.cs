using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext
{
    private static void ConfigureProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>(entity =>
        {
            entity.ToTable("languages");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).ValueGeneratedOnAdd();
            entity.Property(l => l.Name).IsRequired().HasMaxLength(100);
            entity.Property(l => l.Level).IsRequired().HasMaxLength(50);

            entity.HasOne(l => l.User)
                  .WithMany(u => u.Languages)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Certification>(entity =>
        {
            entity.ToTable("certifications");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Entity).HasMaxLength(200);

            entity.HasOne(c => c.User)
                  .WithMany(u => u.Certifications)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Education>(entity =>
        {
            entity.ToTable("education");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Degree).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Institution).HasMaxLength(200);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Educations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(300);
            entity.Property(p => p.Description).HasColumnType("text");
            entity.Property(p => p.Type).IsRequired().HasMaxLength(20);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_projects_type",
                "type IN ('personal', 'school')"));

            entity.HasOne(p => p.User)
                  .WithMany(u => u.Projects)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Keywords)
            .WithMany(k => k.Projects)
            .UsingEntity<Dictionary<string, object>>(
                "project_keywords",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("keyword_id")
                      .OnDelete(DeleteBehavior.Cascade),
                k => k.HasOne<Project>().WithMany().HasForeignKey("project_id")
                      .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("project_id", "keyword_id");
                });

        modelBuilder.Entity<WorkExperience>(entity =>
        {
            entity.ToTable("work_experiences");
            entity.HasKey(w => w.Id);
            entity.Property(w => w.Id).ValueGeneratedOnAdd();
            entity.Property(w => w.Company).IsRequired().HasMaxLength(200);
            entity.Property(w => w.Position).HasMaxLength(200);
            entity.Property(w => w.Description).HasColumnType("text");

            entity.HasOne(w => w.User)
                  .WithMany(u => u.WorkExperiences)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkExperience>()
            .HasMany(w => w.Keywords)
            .WithMany(k => k.WorkExperiences)
            .UsingEntity<Dictionary<string, object>>(
                "work_experience_keywords",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("keyword_id")
                      .OnDelete(DeleteBehavior.Cascade),
                k => k.HasOne<WorkExperience>().WithMany().HasForeignKey("experience_id")
                      .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.HasKey("experience_id", "keyword_id");
                });
    }
}
