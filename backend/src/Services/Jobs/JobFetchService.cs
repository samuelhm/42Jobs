using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models.DTOs;
using src.Services;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs;

public partial class JobFetchService : BackgroundService, IJobFetchService
{
    private readonly Channel<FetchRequest> _channel = Channel.CreateBounded<FetchRequest>(100);
    private readonly ConcurrentDictionary<Guid, FetchStatusDto> _statuses = new();
    private readonly ConcurrentDictionary<int, Guid> _categoryInProgress = new();
    private readonly Dictionary<string, IJobProvider> _providers;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EncryptionService _encryption;
    private readonly ILogger<JobFetchService> _logger;
    private int _fetchAllRunning;

    private static readonly Dictionary<string, string> CompanyTypeMap = new()
    {
        ["Multinational"] = "Multinacional",
        ["Startup"] = "Startup",
        ["SME"] = "Pyme",
        ["Consultancy"] = "Consultora",
    };

    public JobFetchService(IEnumerable<IJobProvider> providers, IServiceScopeFactory scopeFactory, EncryptionService encryption, ILogger<JobFetchService> logger)
    {
        _providers = providers.ToDictionary(p => $"{p.Portal}:{p.ProviderName}");
        _scopeFactory = scopeFactory;
        _encryption = encryption;
        _logger = logger;
    }

    public Guid? Enqueue(int categoryId, string categoryName, FetchRequestDto dto)
    {
        if (_categoryInProgress.TryGetValue(categoryId, out var existingJobId))
            return existingJobId;

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
            jobId, categoryId, categoryName,
            dto.Location, dto.Limit > 0 ? dto.Limit : 10,
            dto.DatePosted, dto.SortBy);

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

    public bool IsCategoryFetching(int categoryId)
        => _categoryInProgress.ContainsKey(categoryId);

    public bool IsFetchAllRunning => Volatile.Read(ref _fetchAllRunning) == 1;

    public QueueStatsDto GetQueueStats()
    {
        var statuses = _statuses.Values;
        var (calls, limit) = GetMonthlyApiUsage();
        return new QueueStatsDto
        {
            Queued = _channel.Reader.Count,
            Running = statuses.Count(s => s.Status == "running"),
            Completed = statuses.Count(s => s.Status == "completed"),
            Failed = statuses.Count(s => s.Status == "failed"),
            FetchAllRunning = IsFetchAllRunning,
            MonthlyApiCalls = calls,
            MonthlyApiLimit = limit,
        };
    }

    private (int Calls, int Limit) GetMonthlyApiUsage()
    {
        int totalCalls = 0, totalLimit = 0;
        foreach (var provider in _providers.Values)
        {
            var (calls, limit) = provider.GetMonthlyStats();
            totalCalls += calls;
            totalLimit += limit;
        }
        return (totalCalls, totalLimit);
    }

    public Task FetchAllCategoriesAsync(string? datePosted = null, string? location = null)
        => FetchAllCategoriesWithTokenAsync(CancellationToken.None, datePosted, location);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobFetchService background service started");

        _ = RunSchedulerAsync(stoppingToken);

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessFetchAsync(request, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in fetch task {JobId}", request.JobId);
                }
            }, stoppingToken);
        }
    }

    private async Task RunSchedulerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Hour < 12
                ? now.Date.AddHours(12)
                : now.Date.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Scheduler: next fetch at {NextRun} UTC (in {Delay})", nextRun, delay);

            try
            {
                await Task.Delay(delay, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await FetchActiveCategoriesAsync(ct);
        }
    }

    private async Task FetchActiveCategoriesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var readiness = scope.ServiceProvider.GetRequiredService<IAiReadinessService>();

            var fetchErrors = new List<string>();
            foreach (var fn in new[] { "filter_jobs", "extract_keywords" })
                fetchErrors.AddRange(await readiness.CheckAsync(fn, ct));

            if (fetchErrors.Count > 0)
            {
                _logger.LogWarning("Scheduled fetch skipped: {Errors}", string.Join("; ", fetchErrors));
                return;
            }

            var fiveDaysAgo = DateTime.UtcNow.AddDays(-5);
            var activeCategories = await db.UserCategories
                .Where(uc => uc.User.LastLoginAt != null && uc.User.LastLoginAt > fiveDaysAgo)
                .Select(uc => new { uc.CategoryId, uc.Category.Name })
                .Distinct()
                .ToListAsync(ct);

            if (activeCategories.Count == 0)
            {
                _logger.LogInformation("Scheduled fetch: no categories with active users (last 5 days)");
                return;
            }

            _logger.LogInformation("Scheduled fetch: {Count} categories with active users", activeCategories.Count);

            foreach (var cat in activeCategories)
            {
                if (ct.IsCancellationRequested) break;

                var jobId = Enqueue(cat.CategoryId, cat.Name, new FetchRequestDto
                {
                    Location = "Spain",
                    Limit = 10,
                    DatePosted = "past-24h",
                    SortBy = "recent",
                });

                if (jobId is not null)
                {
                    while (true)
                    {
                        if (ct.IsCancellationRequested) break;

                        var status = GetStatus(jobId.Value);
                        if (status is null || status.Status is "completed" or "failed")
                            break;

                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    }

                    var category = await db.Categories.FindAsync(new object[] { cat.CategoryId }, ct);
                    if (category is not null)
                    {
                        category.LastFetchedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(ct);
                    }
                }
            }

            _logger.LogInformation("Scheduled fetch completed: {Count} categories processed", activeCategories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled fetch failed");
        }
    }

    private async Task FetchAllCategoriesWithTokenAsync(CancellationToken ct, string? datePosted = null, string? forcedLocation = null)
    {
        var effectiveDatePosted = datePosted ?? "past-24h";

        if (Interlocked.CompareExchange(ref _fetchAllRunning, 1, 0) != 0)
        {
            _logger.LogWarning("Fetch all already running, skipping");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var readiness = scope.ServiceProvider.GetRequiredService<IAiReadinessService>();

            var fetchErrors = new List<string>();
        foreach (var fn in new[] { "filter_jobs", "extract_keywords" })
            fetchErrors.AddRange(await readiness.CheckAsync(fn, ct));

        if (fetchErrors.Count > 0)
        {
            _logger.LogWarning("Scheduled fetch skipped: {Errors}", string.Join("; ", fetchErrors));
            return;
        }

        var categories = await db.Categories.ToListAsync(ct);

        List<string?> locations;
        if (!string.IsNullOrEmpty(forcedLocation))
        {
            locations = new List<string?> { forcedLocation };
        }
        else
        {
            locations = await db.Users
                .Where(u => !string.IsNullOrEmpty(u.PreferredLocation))
                .Select(u => u.PreferredLocation)
                .Distinct()
                .ToListAsync(ct);

            if (locations.Count == 0)
                locations.Add("Barcelona");
        }

        _logger.LogInformation("Scheduled fetch: {Categories} categories × {Locations} locations at {Time} UTC",
            categories.Count, locations.Count, DateTime.UtcNow);

        foreach (var location in locations)
        {
            if (ct.IsCancellationRequested) break;

            foreach (var category in categories)
            {
                if (ct.IsCancellationRequested) break;

                var safeLocation = location!;
                var jobId = Enqueue(category.Id, category.Name, new FetchRequestDto
                {
                    Location = safeLocation,
                    Limit = 10,
                    DatePosted = effectiveDatePosted,
                    SortBy = "recent",
                });

                if (jobId is not null)
                {
                    while (true)
                    {
                        if (ct.IsCancellationRequested) break;

                        var fetchStatus = GetStatus(jobId.Value);
                        if (fetchStatus is null || fetchStatus.Status is "completed" or "failed")
                            break;

                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    }

                    category.LastFetchedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(ct);
                }
                else
                {
                    _logger.LogWarning("FetchAll: failed to enqueue category {Category}", category.Name);
                }
            }
        }
        }
        finally
        {
            Interlocked.Exchange(ref _fetchAllRunning, 0);
        }
    }
}

public record FetchRequest(
    Guid JobId, int CategoryId, string CategoryName,
    string? Location, int Limit, string? DatePosted, string? SortBy);
