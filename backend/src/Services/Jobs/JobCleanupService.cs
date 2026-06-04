using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;

namespace src.Services.Jobs;

public class JobCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobCleanupService> _logger;

    private static readonly string[] ActiveStatuses = ["saved", "cv_enviado", "entrevista_conseguida", "oculto"];

    public JobCleanupService(IServiceScopeFactory scopeFactory, ILogger<JobCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddHours(3);
            if (nextRun <= now)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("JobCleanupService: next run at {NextRun} UTC (in {Delay})", nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await CleanupOldJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "JobCleanupService: error during cleanup");
            }
        }
    }

    private async Task CleanupOldJobsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-15);

        var protectedJobIds = db.UserJobs
            .Where(uj => ActiveStatuses.Contains(uj.Status))
            .Select(uj => uj.JobId)
            .Distinct();

        var oldJobs = await db.Jobs
            .Where(j => j.CreatedAt < cutoff && !protectedJobIds.Contains(j.Id))
            .Include(j => j.Categories)
            .Include(j => j.Company)
            .ToListAsync(ct);

        if (oldJobs.Count == 0)
        {
            _logger.LogInformation("JobCleanupService: no old jobs to clean up");
            return;
        }

        _logger.LogInformation("JobCleanupService: cleaning up {Count} old jobs", oldJobs.Count);

        foreach (var job in oldJobs)
        {
            ct.ThrowIfCancellationRequested();

            var companyName = job.Company?.Name;

            foreach (var category in job.Categories)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO discarded_jobs (external_id, source, title, company_name, location, posted_date, salary, benefits, job_url, description, job_type, experience_level, industry, job_function, applicants, filter_reasons, category_name)
                    VALUES ({job.ExternalId}, {job.Source}, {job.Title}, {companyName}, {job.Location},
                            {job.PostedDate}, {job.Salary}, {job.Benefits}, {job.JobUrl}, {job.Description},
                            {job.JobType}, {job.ExperienceLevel}, {job.Industry}, {job.JobFunction},
                            {job.Applicants}, {"expired:>15days"}, {category.Name})
                    ON CONFLICT (external_id, source, category_name) DO NOTHING");
            }

            db.Jobs.Remove(job);
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("JobCleanupService: deleted {Count} old jobs", oldJobs.Count);
    }
}
