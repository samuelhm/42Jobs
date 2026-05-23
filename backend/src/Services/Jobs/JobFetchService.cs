using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models.DTOs;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs;

public partial class JobFetchService : BackgroundService, IJobFetchService
{
    private readonly Channel<FetchRequest> _channel = Channel.CreateBounded<FetchRequest>(100);
    private readonly ConcurrentDictionary<Guid, FetchStatusDto> _statuses = new();
    private readonly ConcurrentDictionary<int, Guid> _categoryInProgress = new();
    private readonly Dictionary<string, IJobProvider> _providers;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobFetchService> _logger;
    private static int _lastDedupCount;

    private static readonly Dictionary<string, string> CompanyTypeMap = new()
    {
        ["Multinacional"] = "Multinacional",
        ["Startup"] = "Startup",
        ["Pyme"] = "Pyme",
        ["Consultora"] = "Consultora",
    };

    public JobFetchService(IEnumerable<IJobProvider> providers, IServiceScopeFactory scopeFactory, ILogger<JobFetchService> logger)
    {
        _providers = providers.ToDictionary(p => $"{p.Portal}:{p.ProviderName}");
        _scopeFactory = scopeFactory;
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobFetchService background service started");
        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(() => ProcessFetchAsync(request, stoppingToken), stoppingToken);
        }
    }
}

public record FetchRequest(
    Guid JobId, int CategoryId, string CategoryName,
    string? Location, int Limit, string? DatePosted, string? SortBy);
