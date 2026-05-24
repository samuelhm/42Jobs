# How to add a new job provider

42jobs uses a pluggable architecture for job sourcing. Controllers never call a specific API directly. The `JobFetchService` iterates over all **enabled** providers and merges their results.

To add a new provider (e.g., InfoJobs, Apify LinkedIn, Indeed), follow these steps.

## 1. Create the provider class

Create a new folder under `backend/src/Services/Jobs/Providers/`:

```
Providers/InfoJobs/
    InfoJobsProvider.cs
```

Implement `IJobProvider`:

```csharp
using System.Text.Json;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs.Providers.InfoJobs;

public class InfoJobsProvider : IJobProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<InfoJobsProvider> _logger;
    private string? _baseUrlOverride;
    private string? _apiKeyOverride;
    private string? _configJson;

    public static string Portal => "InfoJobs";
    public static string ProviderNameValue => "Native";
    string IJobProvider.Portal => Portal;
    string IJobProvider.ProviderName => ProviderNameValue;

    // These setters are called by JobFetchService with values from the DB
    string? IJobProvider.BaseUrl { set => _baseUrlOverride = value; }
    string? IJobProvider.ApiKey { set => _apiKeyOverride = value; }
    string? IJobProvider.Config { set => _configJson = value; }

    public InfoJobsProvider(IHttpClientFactory httpFactory, ILogger<InfoJobsProvider> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var host = _baseUrlOverride;
        var key = _apiKeyOverride;

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("InfoJobs host not configured. Set it in Admin > Job Providers.");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("InfoJobs API key not configured. Set it in Admin > Job Providers.");

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(host);
        // Add your provider's auth header
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");
        return client;
    }

    public async Task<JobSearchResult> SearchAsync(JobSearchRequest request, CancellationToken ct)
    {
        using var http = CreateClient();

        // Call your provider's search API
        // Map the response to JobSearchResult

        var result = new JobSearchResult();

        // For each job found:
        result.Jobs.Add(new JobItem
        {
            ExternalId = "...",   // unique ID from the source
            Title = "...",
            CompanyName = "...",
            CompanyUrl = "...",
            Location = "...",
            PostedDate = ...,
            Salary = "...",
            Benefits = "...",
            JobUrl = "...",
        });

        result.TotalCount = result.Jobs.Count;
        return result;
    }

    public async Task<JobDetailResult?> GetDetailsAsync(string externalId, CancellationToken ct)
    {
        using var http = CreateClient();

        // Call your provider's detail API
        // Map the response to JobDetailResult

        return new JobDetailResult
        {
            Description = "...",
            JobType = "...",
            ExperienceLevel = "...",
            Industry = "...",
            JobFunction = "...",
            Applicants = "...",
        };
    }
}
```

**Rules:**
- `Portal` must match the portal name in the DB (`job_providers.portal`).
- `ProviderName` must match the provider name in the DB (`job_providers.provider_name`).
- The `IJobProvider` interface exposes setters for `BaseUrl`, `ApiKey`, and `Config`. These are called by `JobFetchService` with values read from the `job_providers` table at runtime. Your provider must store them in private fields and use them when building HTTP requests.
- `ExternalId` is the job's unique identifier from the source. It's used for deduplication and is unique per `(external_id, source)` pair.
- Use `IHttpClientFactory` (not injected `HttpClient`) so the provider can be a singleton.

## 2. Register in DI

In `backend/src/Program.cs`:

```csharp
using src.Services.Jobs.Providers.InfoJobs;

// ...

builder.Services.AddSingleton<IJobProvider, InfoJobsProvider>();
```

The `JobFetchService` picks it up automatically via `IEnumerable<IJobProvider>`.

## 3. Add to the database

Insert a row into `job_providers`:

```sql
INSERT INTO job_providers (portal, provider_name, is_enabled, base_url, api_key) VALUES
    ('InfoJobs', 'Native', TRUE, 'https://api.infojobs.net/', 'your_api_key_here')
ON CONFLICT (portal, provider_name) DO NOTHING;
```

- `is_enabled = TRUE`: the provider will be called on every fetch.
- `api_key`: the API key is stored **directly in the database** (not in environment variables). It is injected into the provider at runtime by `JobFetchService` via the `IJobProvider.ApiKey` setter.
- `base_url`: the base URL for the provider's API. Injected via `IJobProvider.BaseUrl` setter.
- `config`: optional JSON for provider-specific settings (e.g., `{"jobType":"F","remote":"true"}`). Injected via `IJobProvider.Config` setter.
- **Only one provider per portal** should be enabled. The `JobFetchService` enforces this by picking the first enabled one per portal.

## 4. Verify

Restart the backend:

```bash
make dev-restart
```

When a user fetches jobs for a category, the new provider's results will appear alongside LinkedIn's. Deduplication is by `ExternalId` within the same source.

## How results merge

1. `JobFetchService` queries all enabled `job_providers` from DB.
2. It filters to one provider per portal (first enabled wins).
3. Calls `SearchAsync` on each selected provider.
4. Merges all `JobItem` lists into one.
5. Deduplicates by `ExternalId` (within same source, duplicates across sources are kept — same job on LinkedIn and InfoJobs is stored once per source).
6. Processes each job through the AI pipeline (filter, keywords, company type).

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Provider not called | Check `job_providers.is_enabled = TRUE` and `is_active = TRUE` |
| `No DI registration for provider` | The `Portal:ProviderName` combo from DB doesn't match any registered `IJobProvider` |
| Duplicate jobs across sources | Intentional — same job from LinkedIn and InfoJobs are stored separately with different `source` values |
| `401 Unauthorized` | API key missing or invalid in `job_providers.api_key` column |
