using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Data;

public partial class AppDbContext : DbContext
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
    public DbSet<BlockedKeyword> BlockedKeywords => Set<BlockedKeyword>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCategory(modelBuilder);
        ConfigureCompany(modelBuilder);
        ConfigureKeyword(modelBuilder);
        ConfigureJob(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureProfile(modelBuilder);
        ConfigureJobsTracking(modelBuilder);
        ConfigureResume(modelBuilder);
        ConfigureAi(modelBuilder);
        ConfigureInfra(modelBuilder);
    }
}
