using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Services;

public record FetchRequest(
    Guid JobId,
    int CategoryId,
    string CategoryName,
    string? Location,
    int Limit,
    string? DatePosted,
    string? SortBy);

public class JobFetchOrchestrator : BackgroundService
{
    private readonly Channel<FetchRequest> _channel = Channel.CreateBounded<FetchRequest>(100);
    private readonly ConcurrentDictionary<Guid, FetchStatusDto> _statuses = new();
    private readonly ConcurrentDictionary<int, Guid> _categoryInProgress = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobFetchOrchestrator> _logger;

    private static readonly Dictionary<string, string> CompanyTypeMap = new()
    {
        ["Multinacional"] = "Multinacion",
        ["Startup"] = "Startup",
        ["Pyme"] = "Pyme",
        ["Consultora"] = "Consultora",
    };

    public JobFetchOrchestrator(IServiceScopeFactory scopeFactory, ILogger<JobFetchOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Guid? Enqueue(int categoryId, string categoryName, FetchRequestDto dto)
    {
        if (_categoryInProgress.TryGetValue(categoryId, out var existingJobId))
        {
            return existingJobId;
        }

        var jobId = Guid.NewGuid();
        _categoryInProgress[categoryId] = jobId;
        _statuses[jobId] = new FetchStatusDto
        {
            JobId = jobId,
            CategoryId = categoryId,
            CategoryName = categoryName,
            Status = "queued",
        };

        var request = new FetchRequest(
            jobId,
            categoryId,
            categoryName,
            dto.Location,
            dto.Limit > 0 ? dto.Limit : 10,
            dto.DatePosted,
            dto.SortBy);

        if (!_channel.Writer.TryWrite(request))
        {
            _categoryInProgress.TryRemove(categoryId, out _);
            _statuses.TryRemove(jobId, out _);
            return null;
        }

        return jobId;
    }

    public FetchStatusDto? GetStatus(Guid jobId)
    {
        _statuses.TryGetValue(jobId, out var status);
        return status;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobFetchOrchestrator background service started");

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(() => ProcessFetchAsync(request, stoppingToken), stoppingToken);
        }
    }

    private async Task ProcessFetchAsync(FetchRequest request, CancellationToken ct)
    {
        var status = _statuses[request.JobId];
        status.Status = "running";
        _logger.LogInformation("Fetch job {JobId} started for category \"{Category}\"",
            request.JobId, request.CategoryName);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var linkedIn = scope.ServiceProvider.GetRequiredService<LinkedInApiService>();
            var gemini = scope.ServiceProvider.GetRequiredService<GeminiService>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var allJobs = await FetchAllJobsAsync(linkedIn, request, ct);
            var uniqueJobs = DeduplicateJobs(allJobs);

            status.Total = uniqueJobs.Count;
            _logger.LogInformation("Fetch job {JobId}: {Total} unique jobs to process", request.JobId, status.Total);

            var semaphore = new SemaphoreSlim(3);
            var tasks = uniqueJobs.Select(job => Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync(ct);
                    var result = await ProcessJobAsync(
                        scope, linkedIn, gemini, job, request.CategoryId, request.CategoryName, ct);

                    lock (status)
                    {
                        status.Processed++;
                        if (result == "inserted") status.Inserted++;
                        else if (result == "skipped") status.Skipped++;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));

            await Task.WhenAll(tasks);

            status.Status = "completed";
            _logger.LogInformation("Fetch job {JobId} completed: {Total} total, {Inserted} inserted, {Skipped} skipped",
                request.JobId, status.Total, status.Inserted, status.Skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fetch job {JobId} failed", request.JobId);
            status.Status = "failed";
            status.Error = ex.Message;
        }
        finally
        {
            _categoryInProgress.TryRemove(request.CategoryId, out _);
        }
    }

    private async Task<List<JsonElement>> FetchAllJobsAsync(
        LinkedInApiService linkedIn, FetchRequest request, CancellationToken ct)
    {
        var allJobs = new List<JsonElement>();
        var start = 0;
        var limit = request.Limit;

        while (true)
        {
            JsonElement? data;
            try
            {
                data = await linkedIn.SearchJobsAsync(
                    request.CategoryName,
                    request.Location,
                    limit,
                    request.DatePosted,
                    request.SortBy,
                    start,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LinkedIn API page start={Start} failed, breaking pagination", start);
                break;
            }

            if (data is null) break;

            var success = data.Value.TryGetProperty("success", out var s) && s.GetBoolean();
            if (!success) break;

            var count = data.Value.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
            if (count == 0) break;

            if (data.Value.TryGetProperty("jobs", out var jobsArray))
            {
                foreach (var job in jobsArray.EnumerateArray())
                {
                    allJobs.Add(job.Clone());
                }
            }

            if (count < limit) break;
            start += limit;
        }

        return allJobs;
    }

    private static List<JsonElement> DeduplicateJobs(List<JsonElement> jobs)
    {
        var seen = new HashSet<string>();
        var unique = new List<JsonElement>();

        foreach (var job in jobs)
        {
            var id = job.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (id is not null && seen.Add(id))
            {
                unique.Add(job);
            }
        }

        return unique;
    }

    private async Task<string> ProcessJobAsync(
        IServiceScope outerScope,
        LinkedInApiService linkedIn,
        GeminiService gemini,
        JsonElement job,
        int categoryId,
        string categoryName,
        CancellationToken ct)
    {
        using var scope = outerScope.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var jobTitle = job.TryGetProperty("title", out var t) ? t.GetString() ?? "Unknown" : "Unknown";
        var companyName = job.TryGetProperty("company", out var comp) ? comp.GetString() ?? "Unknown" : "Unknown";
        var linkedinId = job.TryGetProperty("id", out var lid) ? lid.GetString() : null;

        if (linkedinId is null) return "skipped";

        var existingJob = await db.Jobs.FirstOrDefaultAsync(j => j.LinkedinId == linkedinId, ct);
        if (existingJob is not null)
        {
            return "skipped";
        }

        JsonElement? details = null;
        try
        {
            details = await linkedIn.GetJobDetailsAsync(linkedinId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to get details for \"{Title}\": {Message}", jobTitle, ex.Message);
        }

        var description = details?.TryGetProperty("description", out var desc) == true
            ? desc.GetString() : null;

        var (relevante, aptoJunior) = await gemini.FilterJobRelevanceAsync(
            categoryName, jobTitle, description, ct);

        if (relevante == "no")
        {
            _logger.LogDebug("Job \"{Title}\" skipped: not relevant", jobTitle);
            return "skipped";
        }
        if (aptoJunior == "no")
        {
            _logger.LogDebug("Job \"{Title}\" skipped: senior only", jobTitle);
            return "skipped";
        }

        var companyId = await UpsertCompanyAsync(db, companyName,
            job.TryGetProperty("companyUrl", out var cu) ? cu.GetString() : null, ct);

        var newJob = new Job
        {
            LinkedinId = linkedinId,
            CompanyId = companyId,
            Title = jobTitle,
            Location = job.TryGetProperty("location", out var loc) ? loc.GetString() : null,
            PostedDate = TryGetDateOnly(job, "postedDate"),
            Salary = job.TryGetProperty("salary", out var sal) ? sal.GetString() : null,
            Benefits = job.TryGetProperty("benefits", out var ben) ? ben.GetString() : null,
            JobUrl = job.TryGetProperty("jobUrl", out var jurl) ? jurl.GetString() : null,
        };

        if (details is not null)
        {
            var d = details.Value;
            newJob.Description = description;
            if (d.TryGetProperty("jobType", out var jt)) newJob.JobType = jt.GetString();
            if (d.TryGetProperty("experienceLevel", out var el)) newJob.ExperienceLevel = el.GetString();
            if (d.TryGetProperty("industry", out var ind)) newJob.Industry = ind.GetString();
            if (d.TryGetProperty("jobFunction", out var jf)) newJob.JobFunction = jf.GetString();
            if (d.TryGetProperty("applicants", out var app)) newJob.Applicants = app.GetString();
        }

        db.Jobs.Add(newJob);
        await db.SaveChangesAsync(ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO job_categories (job_id, category_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
            newJob.Id, categoryId);

        _logger.LogDebug("Job saved: \"{Title}\" (db_id={Id})", jobTitle, newJob.Id);

        try
        {
            var parts = new List<string?> { jobTitle, TryGetString(job, "benefits"), description };
            var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            var (skills, companyType) = await gemini.ExtractKeywordsAsync(inputText, ct);

            foreach (var rawName in skills)
            {
                var name = rawName.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) continue;

                var keywordId = await UpsertKeywordAsync(db, name, ct);
                await LinkJobKeywordAsync(db, newJob.Id, keywordId, ct);
            }

            if (CompanyTypeMap.TryGetValue(companyType, out var mappedType))
            {
                var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == companyName, ct);
                if (company is not null && company.CompanyType is null)
                {
                    company.CompanyType = mappedType;
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword extraction failed for \"{Title}\"", jobTitle);
        }

        return "inserted";
    }

    private static async Task<int> UpsertCompanyAsync(AppDbContext db, string name, string? linkedinUrl, CancellationToken ct)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (company is null)
        {
            company = new Company { Name = name, LinkedinUrl = linkedinUrl };
            db.Companies.Add(company);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is Npgsql.PostgresException pgEx
                      && pgEx.SqlState == "23505")
            {
                db.ChangeTracker.Clear();
                company = await db.Companies.FirstOrDefaultAsync(c => c.Name == name, ct);
                if (company is null) throw;
            }
        }
        else if (linkedinUrl is not null && company.LinkedinUrl is null)
        {
            company.LinkedinUrl = linkedinUrl;
            await db.SaveChangesAsync(ct);
        }
        return company.Id;
    }

    private static async Task<int> UpsertKeywordAsync(AppDbContext db, string name, CancellationToken ct)
    {
        var keyword = await db.Keywords.FirstOrDefaultAsync(k => k.Name == name, ct);
        if (keyword is not null) return keyword.Id;

        keyword = new Keyword { Name = name };
        db.Keywords.Add(keyword);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pgEx
                  && pgEx.SqlState == "23505")
        {
            db.ChangeTracker.Clear();
            keyword = await db.Keywords.FirstOrDefaultAsync(k => k.Name == name, ct);
            if (keyword is null) throw;
        }
        return keyword.Id;
    }

    private static async Task LinkJobKeywordAsync(AppDbContext db, int jobId, int keywordId, CancellationToken ct)
    {
        var exists = await db.Set<Dictionary<string, object>>("job_keywords")
            .AnyAsync(jk => EF.Property<int>(jk, "job_id") == jobId
                         && EF.Property<int>(jk, "keyword_id") == keywordId, ct);
        if (exists) return;

        db.Database.ExecuteSqlRaw(
            "INSERT INTO job_keywords (job_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
            jobId, keywordId);
    }

    private static DateOnly? TryGetDateOnly(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            var str = prop.GetString();
            if (DateOnly.TryParse(str, out var date)) return date;
        }
        return null;
    }

    private static string? TryGetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var prop) ? prop.GetString() : null;
    }
}
