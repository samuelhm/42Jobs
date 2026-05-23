using System.Text.Json;
using src.Services.Jobs.Providers;

namespace src.Services.Jobs.Providers.LinkedIn.RapidApi;

public class LinkedInRapidApiProvider : IJobProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LinkedInRapidApiProvider> _logger;
    private string? _baseUrlOverride;
    private string? _apiKeyOverride;
    private string? _configJson;

    public static string Portal => "LinkedIn";
    public static string ProviderNameValue => "RapidAPI";
    string IJobProvider.Portal => Portal;
    string IJobProvider.ProviderName => ProviderNameValue;
    string? IJobProvider.BaseUrl { set => _baseUrlOverride = value; }
    string? IJobProvider.ApiKey { set => _apiKeyOverride = value; }
    string? IJobProvider.Config { set => _configJson = value; }

    public LinkedInRapidApiProvider(IHttpClientFactory httpFactory, ILogger<LinkedInRapidApiProvider> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private HttpClient CreateClient()
    {
        var host = _baseUrlOverride;
        var key = _apiKeyOverride;

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("LinkedIn host not configured. Set it in Admin > Job Providers.");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("LinkedIn API key not configured. Set it in Admin > Job Providers.");

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri($"https://{host}/");
        client.DefaultRequestHeaders.Add("x-rapidapi-key", key);
        client.DefaultRequestHeaders.Add("x-rapidapi-host", host);
        return client;
    }

    public async Task<JobSearchResult> SearchAsync(JobSearchRequest request, CancellationToken ct)
    {
        using var http = CreateClient();
        var queryParams = new Dictionary<string, string>
        {
            ["keywords"] = request.Keywords,
            ["start"] = request.Start.ToString(),
            ["limit"] = request.Limit.ToString(),
        };

        if (!string.IsNullOrEmpty(request.Location)) queryParams["location"] = request.Location;
        if (!string.IsNullOrEmpty(request.DatePosted)) queryParams["datePosted"] = request.DatePosted;
        if (!string.IsNullOrEmpty(request.SortBy)) queryParams["sortBy"] = request.SortBy;

        if (!string.IsNullOrEmpty(_configJson))
        {
            try
            {
                using var cfg = JsonDocument.Parse(_configJson);
                foreach (var prop in cfg.RootElement.EnumerateObject())
                {
                    var key = prop.Name;
                    var val = prop.Value.GetString();
                    if (string.IsNullOrEmpty(val)) continue;

                    if (key == "location" && string.IsNullOrEmpty(request.Location))
                        queryParams["location"] = val;
                    else if (key == "datePosted" && string.IsNullOrEmpty(request.DatePosted))
                        queryParams["datePosted"] = val;
                    else if (key == "sortBy" && string.IsNullOrEmpty(request.SortBy))
                        queryParams["sortBy"] = val;
                    else if (key is "jobType" or "experienceLevel" or "remote" or "companySize" or "industry" or "jobFunction" or "salary")
                        queryParams[key] = val;
                }
            }
            catch (JsonException) { }
        }

        var qs = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var response = await http.GetAsync($"/search?{qs}", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
        var count = root.TryGetProperty("count", out var c) ? c.GetInt32() : 0;

        var result = new JobSearchResult { TotalCount = count };

        if (success && root.TryGetProperty("jobs", out var jobsArray))
        {
            foreach (var job in jobsArray.EnumerateArray())
            {
                result.Jobs.Add(new JobItem
                {
                    ExternalId = job.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                    Title = job.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    CompanyName = job.TryGetProperty("company", out var comp) ? comp.GetString() ?? "" : "",
                    CompanyUrl = job.TryGetProperty("companyUrl", out var cu) ? cu.GetString() : null,
                    Location = job.TryGetProperty("location", out var loc) ? loc.GetString() : null,
                    PostedDate = TryGetDateOnly(job, "postedDate"),
                    Salary = job.TryGetProperty("salary", out var sal) ? sal.GetString() : null,
                    Benefits = job.TryGetProperty("benefits", out var ben) ? ben.GetString() : null,
                    JobUrl = job.TryGetProperty("jobUrl", out var jurl) ? jurl.GetString() : null,
                });
            }
        }

        _logger.LogDebug("LinkedIn RapidAPI search: start={Start}, count={Count}",
            request.Start, result.Jobs.Count);

        return result;
    }

    public async Task<JobDetailResult?> GetDetailsAsync(string externalId, CancellationToken ct)
    {
        using var http = CreateClient();
        var response = await http.GetAsync($"/job/{Uri.EscapeDataString(externalId)}", ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var success) && success.GetBoolean()
            && root.TryGetProperty("job", out var jobData))
        {
            var d = jobData;
            return new JobDetailResult
            {
                Description = d.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                JobType = d.TryGetProperty("jobType", out var jt) ? jt.GetString() : null,
                ExperienceLevel = d.TryGetProperty("experienceLevel", out var el) ? el.GetString() : null,
                Industry = d.TryGetProperty("industry", out var ind) ? ind.GetString() : null,
                JobFunction = d.TryGetProperty("jobFunction", out var jf) ? jf.GetString() : null,
                Applicants = d.TryGetProperty("applicants", out var app) ? app.GetString() : null,
            };
        }

        return null;
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
}
