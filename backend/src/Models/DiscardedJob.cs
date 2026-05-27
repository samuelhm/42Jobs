namespace src.Models;

public class DiscardedJob
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = "linkedin";
    public string? Title { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public DateOnly? PostedDate { get; set; }
    public string? Salary { get; set; }
    public string? Benefits { get; set; }
    public string? JobUrl { get; set; }
    public string? Description { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? Industry { get; set; }
    public string? JobFunction { get; set; }
    public string? Applicants { get; set; }
    public string? FilterReasons { get; set; }
    public string? CategoryName { get; set; }
    public string? RawData { get; set; }
    public DateTime CreatedAt { get; set; }
}
