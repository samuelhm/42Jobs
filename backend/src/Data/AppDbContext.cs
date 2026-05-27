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
    public DbSet<UserCategory> UserCategories => Set<UserCategory>();
    public DbSet<UserJob> UserJobs => Set<UserJob>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<UserKeyword> UserKeywords => Set<UserKeyword>();
    public DbSet<AiService> AiServices => Set<AiService>();
    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<AiPrompt> AiPrompts => Set<AiPrompt>();
    public DbSet<CvTemplate> CvTemplates => Set<CvTemplate>();
    public DbSet<JobProvider> JobProviders => Set<JobProvider>();
    public DbSet<DiscardedJob> DiscardedJobs => Set<DiscardedJob>();
    public DbSet<AdminLog> AdminLogs => Set<AdminLog>();

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
            entity.Property(c => c.LastFetchedAt);
        });

        // ── Company ──────────────────────────────────────────
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Name).IsRequired().HasMaxLength(500);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.Property(c => c.WebsiteUrl).HasColumnType("text");
            entity.Property(c => c.CompanyType).HasMaxLength(50);
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_companies_company_type",
                "company_type IN ('Multinacional', 'Startup', 'Pyme', 'Consultora')"));
        });

        // ── Keyword ──────────────────────────────────────────
        modelBuilder.Entity<Keyword>(entity =>
        {
            entity.ToTable("keywords");
            entity.HasKey(k => k.Id);
            entity.Property(k => k.Id).ValueGeneratedOnAdd();
            entity.Property(k => k.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(k => k.Name).IsUnique();
            entity.Property(k => k.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
        });

        // ── Job ──────────────────────────────────────────────
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

        // ── JobCategory (M2M: jobs ↔ categories) ──────────────
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
            entity.ToTable("users", t => t.HasCheckConstraint(
                "CK_users_role",
                "role IN ('Admin', 'User')"));
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
            entity.Property(u => u.PreferredLocation).HasMaxLength(200);
            entity.Property(u => u.Role)
                  .HasMaxLength(20)
                  .HasDefaultValue("User");
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

        // ── UserCategory ──────────────────────────────────────
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
                  .HasColumnType("text")
                  .HasDefaultValue("");
            entity.Property(r => r.JsonData).HasColumnType("jsonb");
            entity.Property(r => r.Model)
                  .HasMaxLength(30)
                  .HasDefaultValue("gpt-5.4-mini");
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

            entity.HasOne(r => r.Template)
                  .WithMany(t => t.Resumes)
                  .HasForeignKey(r => r.TemplateId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Prompt)
                  .WithMany(p => p.Resumes)
                  .HasForeignKey(r => r.PromptId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.AiModel)
                  .WithMany(m => m.Resumes)
                  .HasForeignKey(r => r.ModelId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── UserKeyword (M2M: users ↔ keywords) ──────────────
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

        // ── AiService ─────────────────────────────────────────
        modelBuilder.Entity<AiService>(entity =>
        {
            entity.ToTable("ai_services");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Id).ValueGeneratedOnAdd();
            entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(s => s.Name).IsUnique();
            entity.Property(s => s.ApiKey).HasMaxLength(500);
            entity.Property(s => s.IsActive).HasDefaultValue(true);
            entity.Property(s => s.IsFreeTier).HasDefaultValue(false);
            entity.Property(s => s.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(s => s.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();
        });

        // ── AiModel ───────────────────────────────────────────
        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.ToTable("ai_models");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).ValueGeneratedOnAdd();
            entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
            entity.Property(m => m.IsActive).HasDefaultValue(true);
            entity.Property(m => m.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.HasIndex(m => new { m.AiServiceId, m.Name }).IsUnique();
            entity.HasIndex(m => m.AiServiceId).HasDatabaseName("idx_ai_models_service");

            entity.HasOne(m => m.AiService)
                  .WithMany(s => s.Models)
                  .HasForeignKey(m => m.AiServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── AiPrompt ──────────────────────────────────────────
        modelBuilder.Entity<AiPrompt>(entity =>
        {
            entity.ToTable("ai_prompts");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedOnAdd();
            entity.Property(p => p.Functionality).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.Functionality).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasColumnType("text");
            entity.Property(p => p.SystemPrompt).IsRequired().HasColumnType("text");
            entity.Property(p => p.UserPromptTemplate).IsRequired().HasColumnType("text");
            entity.Property(p => p.IsActive).HasDefaultValue(true);
            entity.Property(p => p.CreatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAdd();
            entity.Property(p => p.UpdatedAt)
                  .HasDefaultValueSql("NOW()")
                  .ValueGeneratedOnAddOrUpdate();

            entity.HasIndex(p => p.DefaultModelId).HasDatabaseName("idx_ai_prompts_model");
            entity.HasOne(p => p.DefaultModel)
                  .WithMany()
                  .HasForeignKey(p => p.DefaultModelId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── CvTemplate ────────────────────────────────────────
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

        // ── JobProvider ──────────────────────────────────────
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

        // ── DiscardedJob ──────────────────────────────────────
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

        // ── AdminLog ───────────────────────────────────────────
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
    }
}
