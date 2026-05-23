using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using src.Data;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs;

public partial class JobFetchService
{
    private async Task ProcessFetchAsync(FetchRequest request, CancellationToken ct)
    {
        var status = _statuses[request.JobId];
        status.Status = "running";
        _logger.LogInformation("Fetch job {JobId} started for \"{Category}\"", request.JobId, request.CategoryName);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ai = scope.ServiceProvider.GetRequiredService<IAiService>();

            var enabledProviders = await GetEnabledProvidersAsync(db);
            var allJobs = new List<JobItem>();

            foreach (var provider in enabledProviders)
            {
                try
                {
                    var start = 0;
                    var limit = request.Limit > 0 ? request.Limit : 10;

                    while (true)
                    {
                        var result = await provider.SearchAsync(
                            new JobSearchRequest(request.CategoryName, request.Location, limit,
                                request.DatePosted, request.SortBy, start), ct);

                        allJobs.AddRange(result.Jobs);
                        _logger.LogDebug("Fetched {Count} jobs from {Portal}:{Provider} (start={Start})",
                            result.Jobs.Count, provider.Portal, provider.ProviderName, start);

                        if (result.Jobs.Count < limit) break;
                        start += limit;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Portal}:{Provider} search failed",
                        provider.Portal, provider.ProviderName);
                }
            }

            var uniqueJobs = DeduplicateJobs(allJobs);
            status.Total = uniqueJobs.Count;
            _logger.LogInformation("Fetch {JobId}: {Total} unique jobs", request.JobId, status.Total);

            var isFreeTier = await db.AiServices.AnyAsync(s => s.IsFreeTier && s.IsActive, ct);
            var maxParallel = isFreeTier ? 1 : 3;
            var semaphore = new SemaphoreSlim(maxParallel);
            var tasks = uniqueJobs.Select(job => Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync(ct);
                    var result = await ProcessJobAsync(scope, enabledProviders, ai, job,
                        request.CategoryId, request.CategoryName, ct);

                    lock (status)
                    {
                        status.Processed++;
                        if (result == "inserted") status.Inserted++;
                        else if (result == "skipped") status.Skipped++;
                    }
                }
                finally { semaphore.Release(); }
            }, ct));

            await Task.WhenAll(tasks);

            status.Status = "completed";
            _logger.LogInformation("Fetch {JobId} done: {Inserted} inserted, {Skipped} skipped",
                request.JobId, status.Inserted, status.Skipped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fetch {JobId} failed", request.JobId);
            status.Status = "failed";
            status.Error = ex.Message;
        }
        finally
        {
            _categoryInProgress.TryRemove(request.CategoryId, out _);
        }
    }

    private async Task<List<IJobProvider>> GetEnabledProvidersAsync(AppDbContext db)
    {
        var enabled = await db.Set<Models.JobProvider>()
            .Where(p => p.IsEnabled && p.IsActive)
            .OrderBy(p => p.Portal)
            .ToListAsync();

        var result = new List<IJobProvider>();
        var seenPortals = new HashSet<string>();

        foreach (var config in enabled)
        {
            if (!seenPortals.Add(config.Portal)) continue;

            var key = $"{config.Portal}:{config.ProviderName}";
            if (_providers.TryGetValue(key, out var provider))
            {
                provider.BaseUrl = config.BaseUrl;
                provider.ApiKey = config.ApiKey;
                provider.Config = config.Config;
                result.Add(provider);
            }
            else
                _logger.LogWarning("No DI registration for provider {Key}", key);
        }

        return result;
    }

    private static List<JobItem> DeduplicateJobs(List<JobItem> jobs)
    {
        var seen = new HashSet<string>();
        var unique = new List<JobItem>();

        foreach (var job in jobs)
        {
            if (seen.Add(job.ExternalId))
                unique.Add(job);
        }

        return unique;
    }
}
