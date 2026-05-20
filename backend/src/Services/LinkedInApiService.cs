using System.Text.Json;

namespace src.Services;

public class LinkedInApiService
{
    private readonly HttpClient _http;
    private readonly ILogger<LinkedInApiService> _logger;

    public LinkedInApiService(HttpClient http, ILogger<LinkedInApiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<JsonElement?> SearchJobsAsync(
        string keywords,
        string? location,
        int limit,
        string? datePosted,
        string? sortBy,
        int start,
        CancellationToken ct = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["keywords"] = keywords,
            ["start"] = start.ToString(),
            ["limit"] = limit.ToString(),
        };

        if (!string.IsNullOrEmpty(location)) queryParams["location"] = location;
        if (!string.IsNullOrEmpty(datePosted)) queryParams["datePosted"] = datePosted;
        if (!string.IsNullOrEmpty(sortBy)) queryParams["sortBy"] = sortBy;

        var qs = string.Join("&", queryParams.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var url = $"/search?{qs}";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _logger.LogDebug("LinkedIn search page start={Start} -> {Count} jobs",
            start, root.TryGetProperty("count", out var c) ? c.GetInt32() : 0);

        return root.Clone();
    }

    public async Task<JsonElement?> GetJobDetailsAsync(string jobId, CancellationToken ct = default)
    {
        var url = $"/job/{Uri.EscapeDataString(jobId)}";

        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
