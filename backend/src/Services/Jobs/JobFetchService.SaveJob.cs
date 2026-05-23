using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using src.Data;
using src.Models;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs;

public partial class JobFetchService
{
    private async Task<string> ProcessJobAsync(
        IServiceScope outerScope,
        List<IJobProvider> providers,
        IAiService ai,
        JobItem job,
        int categoryId,
        string categoryName,
        CancellationToken ct)
    {
        using var scope = outerScope.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (string.IsNullOrEmpty(job.ExternalId)) return "skipped";

        var source = "linkedin"; // TODO: get from provider when multi-source
        var existingJob = await db.Jobs.FirstOrDefaultAsync(
            j => j.ExternalId == job.ExternalId && j.Source == source, ct);
        if (existingJob is not null) return "skipped";

        JobDetailResult? details = null;
        foreach (var provider in providers)
        {
            try
            {
                details = await provider.GetDetailsAsync(job.ExternalId, ct);
                if (details is not null) break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get details for \"{Title}\" from {Provider}",
                    job.Title, provider.ProviderName);
            }
        }

        var description = details?.Description;
        var (relevant, juniorFriendly) = await ai.FilterJobRelevanceAsync(
            categoryName, job.Title, description, ct);

        if (relevant == "no")
        {
            _logger.LogDebug("Job \"{Title}\" skipped: not relevant", job.Title);
            return "skipped";
        }
        if (juniorFriendly == "no")
        {
            _logger.LogDebug("Job \"{Title}\" skipped: senior only", job.Title);
            return "skipped";
        }

        var companyId = await UpsertCompanyAsync(db, job.CompanyName, job.CompanyUrl, ct);

        var newJob = new Job
        {
            ExternalId = job.ExternalId,
            Source = source,
            CompanyId = companyId,
            Title = job.Title,
            Location = job.Location,
            PostedDate = job.PostedDate,
            Salary = job.Salary,
            Benefits = job.Benefits,
            JobUrl = job.JobUrl,
        };

        if (details is not null)
        {
            newJob.Description = details.Description;
            newJob.JobType = details.JobType;
            newJob.ExperienceLevel = details.ExperienceLevel;
            newJob.Industry = details.Industry;
            newJob.JobFunction = details.JobFunction;
            newJob.Applicants = details.Applicants;
        }

        db.Jobs.Add(newJob);
        await db.SaveChangesAsync(ct);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO job_categories (job_id, category_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
            newJob.Id, categoryId);

        _logger.LogDebug("Job saved: \"{Title}\" (id={Id})", job.Title, newJob.Id);

        try
        {
            var parts = new List<string?> { job.Title, job.Benefits, description };
            var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            var (skills, companyType) = await ai.ExtractKeywordsAsync(inputText, ct);

            foreach (var rawName in skills)
            {
                var name = rawName.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) continue;
                var keywordId = await UpsertKeywordAsync(db, name, ct);
                await LinkJobKeywordAsync(db, newJob.Id, keywordId, ct);
            }

            if (CompanyTypeMap.TryGetValue(companyType, out var mappedType))
            {
                var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == job.CompanyName, ct);
                if (company is not null && company.CompanyType is null)
                {
                    company.CompanyType = mappedType;
                    await db.SaveChangesAsync(ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword extraction failed for \"{Title}\"", job.Title);
        }

        return "inserted";
    }

    private static async Task<int> UpsertCompanyAsync(AppDbContext db, string name, string? websiteUrl, CancellationToken ct)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == name, ct);
        if (company is null)
        {
            company = new Company { Name = name, WebsiteUrl = websiteUrl };
            db.Companies.Add(company);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                db.ChangeTracker.Clear();
                company = await db.Companies.FirstOrDefaultAsync(c => c.Name == name, ct);
                if (company is null) throw;
            }
        }
        else if (websiteUrl is not null && company.WebsiteUrl is null)
        {
            company.WebsiteUrl = websiteUrl;
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
            when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
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
}
