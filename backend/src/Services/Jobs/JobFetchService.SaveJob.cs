using System.Text.Json;
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

        var source = "linkedin";
        var existingJob = await db.Jobs.FirstOrDefaultAsync(
            j => j.ExternalId == job.ExternalId && j.Source == source, ct);
        if (existingJob is not null) return "skipped";

        var discarded = await db.DiscardedJobs.FirstOrDefaultAsync(
            d => d.ExternalId == job.ExternalId && d.Source == source && d.CategoryName == categoryName, ct);
        if (discarded is not null)
        {
            _logger.LogDebug("Job \"{Title}\" already discarded, skipping", job.Title);
            return "skipped";
        }

        var details = await GetDetailsWithRetryAsync(providers, job, ct);
        if (details is null)
        {
            _logger.LogWarning("GetDetails failed after all retries for '{Title}', skipping", job.Title);
            return "skipped";
        }

        var seniorLevels = new[] { "Mid-Senior level", "Director", "Executive" };
        if (!string.IsNullOrEmpty(details.ExperienceLevel)
            && seniorLevels.Contains(details.ExperienceLevel, StringComparer.OrdinalIgnoreCase))
        {
            var rawData = JsonSerializer.Serialize(new
            {
                search = new
                {
                    job.ExternalId, job.Title, job.CompanyName, job.CompanyUrl,
                    job.Location, job.PostedDate, job.Salary, job.Benefits, job.JobUrl
                },
                details = new
                {
                    details.Description, details.JobType, details.ExperienceLevel,
                    details.Industry, details.JobFunction, details.Applicants
                }
            });

            db.DiscardedJobs.Add(new DiscardedJob
            {
                ExternalId = job.ExternalId,
                Source = source,
                Title = job.Title,
                CompanyName = job.CompanyName,
                Location = job.Location,
                PostedDate = job.PostedDate,
                Salary = job.Salary,
                Benefits = job.Benefits,
                JobUrl = job.JobUrl,
                Description = details.Description,
                JobType = details.JobType,
                ExperienceLevel = details.ExperienceLevel,
                Industry = details.Industry,
                JobFunction = details.JobFunction,
                Applicants = details.Applicants,
                FilterReasons = JsonSerializer.Serialize(new { relevant = "yes", juniorFriendly = "no" }),
                CategoryName = categoryName,
                RawData = rawData,
            });
            await db.SaveChangesAsync(ct);

            _logger.LogDebug("Job \"{Title}\" discarded: experience_level={Level}", job.Title, details.ExperienceLevel);
            return "skipped";
        }

        var (relevant, juniorFriendly) = await FilterWithRetryAsync(ai, categoryName, job.Title, details.Description, ct);

        if (relevant == "no" || juniorFriendly == "no")
        {
            var reasons = JsonSerializer.Serialize(new { relevant, juniorFriendly });
            var rawData = JsonSerializer.Serialize(new
            {
                search = new
                {
                    job.ExternalId, job.Title, job.CompanyName, job.CompanyUrl,
                    job.Location, job.PostedDate, job.Salary, job.Benefits, job.JobUrl
                },
                details = new
                {
                    details.Description, details.JobType, details.ExperienceLevel,
                    details.Industry, details.JobFunction, details.Applicants
                }
            });

            db.DiscardedJobs.Add(new DiscardedJob
            {
                ExternalId = job.ExternalId,
                Source = source,
                Title = job.Title,
                CompanyName = job.CompanyName,
                Location = job.Location,
                PostedDate = job.PostedDate,
                Salary = job.Salary,
                Benefits = job.Benefits,
                JobUrl = job.JobUrl,
                Description = details.Description,
                JobType = details.JobType,
                ExperienceLevel = details.ExperienceLevel,
                Industry = details.Industry,
                JobFunction = details.JobFunction,
                Applicants = details.Applicants,
                FilterReasons = reasons,
                CategoryName = categoryName,
                RawData = rawData,
            });
            await db.SaveChangesAsync(ct);

            _logger.LogDebug("Job \"{Title}\" discarded: relevant={Relevant}, junior={Junior}", job.Title, relevant, juniorFriendly);
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
            Description = details.Description,
            JobType = details.JobType,
            ExperienceLevel = details.ExperienceLevel,
            Industry = details.Industry,
            JobFunction = details.JobFunction,
            Applicants = details.Applicants,
        };

        db.Jobs.Add(newJob);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
        {
            db.ChangeTracker.Clear();
            _logger.LogDebug("Job \"{Title}\" already exists (race), skipping", job.Title);
            return "skipped";
        }

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO job_categories (job_id, category_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
            newJob.Id, categoryId);

        _logger.LogDebug("Job saved: \"{Title}\" (id={Id})", job.Title, newJob.Id);

        var parts = new List<string?> { job.Title, job.Benefits, details.Description };
        var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        await ExtractKeywordsWithRetryAsync(ai, db, newJob.Id, job, inputText, ct);

        return "inserted";
    }

    private async Task<JobDetailResult?> GetDetailsWithRetryAsync(
        List<IJobProvider> providers, JobItem job, CancellationToken ct)
    {
        for (var retry = 0; retry < 10; retry++)
        {
            foreach (var provider in providers)
            {
                try
                {
                    var details = await provider.GetDetailsAsync(job.ExternalId, ct);
                    if (details is not null && !string.IsNullOrEmpty(details.Description))
                        return details;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GetDetails attempt {Attempt} for \"{Title}\" from {Provider} failed",
                        retry + 1, job.Title, provider.ProviderName);
                }
            }

            if (retry < 9)
            {
                var delay = (int)Math.Pow(2, retry) * 1200 + Random.Shared.Next(800);
                _logger.LogWarning("GetDetails for \"{Title}\" returned no description, retry {Retry}/10 in {Delay}ms",
                    job.Title, retry + 1, delay);
                await Task.Delay(delay, ct);
            }
        }

        return null;
    }

    private async Task<(string relevant, string juniorFriendly)> FilterWithRetryAsync(
        IAiService ai, string categoryName, string title, string? description, CancellationToken ct)
    {
        var hadError = false;
        string lastRelevant = "yes";
        string lastJuniorFriendly = "yes";

        for (var retry = 0; retry < 3; retry++)
        {
            try
            {
                var (relevant, juniorFriendly) = await ai.FilterJobRelevanceAsync(
                    categoryName, title, description, ct);

                lastRelevant = relevant;
                lastJuniorFriendly = juniorFriendly;

                if (relevant is "yes" or "no" && juniorFriendly is "yes" or "no")
                    return (relevant, juniorFriendly);

                _logger.LogWarning("AI filter returned unexpected values: relevant='{Relevant}' junior='{Junior}' for \"{Title}\", attempt {Attempt}",
                    relevant, juniorFriendly, title, retry + 1);

                if (retry < 2)
                {
                    var delay = (int)Math.Pow(2, retry) * 2000 + Random.Shared.Next(1000);
                    await Task.Delay(delay, ct);
                }
            }
            catch (Exception ex)
            {
                hadError = true;

                if (retry < 2)
                {
                    var delay = (int)Math.Pow(2, retry) * 2000 + Random.Shared.Next(1000);
                    _logger.LogWarning(ex, "AI filter attempt {Attempt}/3 failed for \"{Title}\", retrying in {Delay}ms",
                        retry + 1, title, delay);
                    await Task.Delay(delay, ct);
                }
                else
                {
                    _logger.LogWarning(ex, "AI filter failed after 3 attempts for \"{Title}\", skipping job", title);
                }
            }
        }

        if (hadError)
            throw new InvalidOperationException($"AI filter failed after 3 attempts for \"{title}\"");

        var finalRelevant = lastRelevant is "yes" or "no" ? lastRelevant : "yes";
        var finalJunior = lastJuniorFriendly is "yes" or "no" ? lastJuniorFriendly : "yes";

        _logger.LogWarning("AI filter could not decide for \"{Title}\" after 3 attempts, using last values: relevant='{Relevant}' junior='{Junior}'",
            title, finalRelevant, finalJunior);
        return (finalRelevant, finalJunior);
    }

    private async Task ExtractKeywordsWithRetryAsync(
        IAiService ai, AppDbContext db, int jobId, JobItem job, string inputText, CancellationToken ct)
    {
        for (var retry = 0; retry < 3; retry++)
        {
            try
            {
                var (skills, companyType) = await ai.ExtractKeywordsAsync(inputText, ct);

                foreach (var rawName in skills)
                {
                    var name = rawName.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(name)) continue;
                    var keywordId = await UpsertKeywordAsync(db, name, ct);
                    await LinkJobKeywordAsync(db, jobId, keywordId, ct);
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

                return;
            }
            catch (Exception ex)
            {
                if (retry < 2)
                {
                    var delay = (int)Math.Pow(2, retry) * 2000 + Random.Shared.Next(1000);
                    _logger.LogWarning(ex, "Keyword extraction attempt {Attempt}/3 failed for \"{Title}\", retrying in {Delay}ms",
                        retry + 1, job.Title, delay);
                    await Task.Delay(delay, ct);
                }
                else
                {
                    _logger.LogWarning(ex, "Keyword extraction failed after 3 attempts for \"{Title}\"", job.Title);
                }
            }
        }
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
