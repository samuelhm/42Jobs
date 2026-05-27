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

    public Task FetchAllCategoriesAsync() => FetchAllCategoriesWithTokenAsync(CancellationToken.None);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobFetchService background service started");

        _ = RunSchedulerAsync(stoppingToken);

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(() => ProcessFetchAsync(request, stoppingToken), stoppingToken);
        }
    }

    private async Task RunSchedulerAsync(CancellationToken ct)
    {
        var targetHours = new[] { 8, 12, 16, 20 };

        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date;

            foreach (var hour in targetHours)
            {
                var candidate = now.Date.AddHours(hour);
                if (candidate > now)
                {
                    nextRun = candidate;
                    break;
                }
            }

            if (nextRun <= now)
                nextRun = now.Date.AddDays(1).AddHours(targetHours[0]);

            var delay = nextRun - now;
            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation("Scheduler: next run at {NextRun} UTC (in {Delay})", nextRun, delay);
                await Task.Delay(delay, ct);
            }

            if (ct.IsCancellationRequested) break;

            await FetchAllCategoriesWithTokenAsync(ct);

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }

    private async Task FetchAllCategoriesWithTokenAsync(CancellationToken ct)
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

        var locations = await db.Users
            .Where(u => !string.IsNullOrEmpty(u.PreferredLocation))
            .Select(u => u.PreferredLocation)
            .Distinct()
            .ToListAsync(ct);

        if (locations.Count == 0)
            locations.Add("Barcelona");

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
                    DatePosted = "past-24h",
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
                    _logger.LogWarning("Scheduler: failed to enqueue category {Category}", category.Name);
                }
            }
        }
    }
}

public record FetchRequest(
    Guid JobId, int CategoryId, string CategoryName,
    string? Location, int Limit, string? DatePosted, string? SortBy);
