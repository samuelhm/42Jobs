namespace src.Services.Jobs.Providers;

public interface IJobProvider
{
    string Portal { get; }
    string ProviderName { get; }
    Task<JobSearchResult> SearchAsync(JobSearchRequest request, ProviderConfig config, CancellationToken ct);
    Task<JobDetailResult?> GetDetailsAsync(string externalId, ProviderConfig config, CancellationToken ct);

    (int Calls, int Limit) GetMonthlyStats() => (0, 50000);
}

public record ProviderConfig(string? BaseUrl, string? ApiKey, string? ConfigJson);

public record JobSearchRequest(
    string Keywords,
    string? Location,
    int Limit,
    string? DatePosted,
    string? SortBy,
    int Start
);

public class JobSearchResult
{
    public List<JobItem> Jobs { get; set; } = [];
    public int TotalCount { get; set; }
}

public class JobItem
{
    public string ExternalId { get; set; } = "";
    public string Title { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? CompanyUrl { get; set; }
    public string? Location { get; set; }
    public DateOnly? PostedDate { get; set; }
    public string? Salary { get; set; }
    public string? Benefits { get; set; }
    public string? JobUrl { get; set; }
    public string Source { get; set; } = "linkedin";
}

public class JobDetailResult
{
    public string? Description { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? Industry { get; set; }
    public string? JobFunction { get; set; }
    public string? Applicants { get; set; }
}
