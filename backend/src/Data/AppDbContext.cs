using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public class AppDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Keyword> Keywords => Set<Keyword>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<UserProvider> UserProviders => Set<UserProvider>();
    public DbSet<UserJob> UserJobs => Set<UserJob>();
    public DbSet<Resume> Resumes => Set<Resume>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Category ─────────────────────────────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        // ── Company ──────────────────────────────────────────
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(500);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.LinkedinUrl).HasColumnType("text");
            entity.Property(c => c.CompanyType).HasMaxLength(50);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_companies_company_type",
                "company_type IN ('Multinacion', 'Startup', 'Pyme', 'Consultora')"));
        });

        // ── Keyword ──────────────────────────────────────────
        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.ToTable("keywords");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).ValueGeneratedOnAdd();
            entity.Property(k => k.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(k => k.Name).IsUnique();
            entity.Property(k => k.LearningStatus)
                  .IsRequired()
                  .HasMaxLength(50)
                  .HasDefaultValue("not_learned");
            entity.Property(k => k.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_keywords_learning_status",
                "learning_status IN ('not_learned', 'learned_personal_project', 'learned_in_school')"));
        });

        // ── Job ──────────────────────────────────────────────
        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("jobs");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Id).ValueGeneratedOnAdd();
            entity.Property(j => j.LinkedinId).IsRequired().HasMaxLength(50);
            entity.HasIndex(j => j.LinkedinId).IsUnique();
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

            entity.HasOne(j => j.Category)
                  .WithMany(c => c.Jobs)
                  .HasForeignKey(j => j.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(j => j.Company)
                  .WithMany(c => c.Jobs)
                  .HasForeignKey(j => j.CompanyId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(j => j.CategoryId);
            entity.HasIndex(j => j.CompanyId);
            entity.HasIndex(j => j.PostedDate);
        });

        // ── JobKeyword (M2M: jobs ↔ keywords) ───────────────
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

        // ── User ─────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                  .HasDefaultValueSql("gen_random_uuid()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(300);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(300);
            entity.Property(u => u.Name).HasMaxLength(200);
            entity.Property(u => u.LastName).HasMaxLength(200);
            entity.Property(u => u.Phone).HasMaxLength(50);
            entity.Property(u => u.Address).HasColumnType("text");
            entity.Property(u => u.LinkedinUrl).HasColumnType("text");
            entity.Property(u => u.WebsiteUrl).HasColumnType("text");
            entity.Property(u => u.GithubUrl).HasColumnType("text");
            entity.Property(u => u.Junior).HasDefaultValue(true);
            entity.Property(u => u.Presentation).HasColumnType("text");
            entity.Property(u => u.AvatarUrl).HasColumnType("text");
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });

        // ── Language ─────────────────────────────────────────
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

        // ── Certification ────────────────────────────────────
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

        // ── Education ────────────────────────────────────────
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

        // ── Project ──────────────────────────────────────────
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

        // ── ProjectKeyword (M2M: projects ↔ keywords) ───────
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

        // ── WorkExperience ───────────────────────────────────
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

        // ── WorkExperienceKeyword (M2M: work_experiences ↔ keywords) ──
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

        // ── UserProvider ─────────────────────────────────────
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

        // ── UserJob ──────────────────────────────────────────
        modelBuilder.Entity<UserJob>(entity =>
        {
            entity.ToTable("user_jobs");
            entity.HasKey(u => new { u.UserId, u.JobId });
            entity.Property(u => u.SavedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(u => u.Notes).HasColumnType("text");
            entity.Property(u => u.Applied).HasDefaultValue(false);
            entity.Property(u => u.AppliedAt);

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

        // ── Resume ───────────────────────────────────────────
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.ToTable("resumes");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id)
                  .HasDefaultValueSql("gen_random_uuid()")
                  .ValueGeneratedOnAdd();
            entity.Property(r => r.CvData)
                  .IsRequired()
                  .HasColumnType("jsonb")
                  .HasDefaultValueSql("'{}'::jsonb");
            entity.Property(r => r.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(r => r.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
            entity.HasIndex(r => new { r.UserId, r.JobId }).IsUnique();
            entity.HasIndex(r => r.UserId).HasDatabaseName("idx_resumes_user");
            entity.HasIndex(r => r.JobId).HasDatabaseName("idx_resumes_job");

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Resumes)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Job)
                  .WithMany(j => j.Resumes)
                  .HasForeignKey(r => r.JobId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
