namespace src.Services.Jobs.Providers;

public interface IJobProvider
{
    string Portal { get; }
    string ProviderName { get; }
    string? BaseUrl { set; }
    string? ApiKey { set; }
    string? Config { set; }
    Task<JobSearchResult> SearchAsync(JobSearchRequest request, CancellationToken ct);
    Task<JobDetailResult?> GetDetailsAsync(string externalId, CancellationToken ct);
}

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
