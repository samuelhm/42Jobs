namespace src.Models;

public class Job
{
    public int Id { get; set; }
    public string LinkedinId { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    public DateOnly? PostedDate { get; set; }
    public string? Salary { get; set; }
    public string? Benefits { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? Industry { get; set; }
    public string? JobFunction { get; set; }
    public string? Applicants { get; set; }
    public string? Description { get; set; }
    public string? JobUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Category> Categories { get; set; } = [];
    public Company? Company { get; set; }
    public List<Keyword> Keywords { get; set; } = [];
    public List<UserJob> UserJobs { get; set; } = [];
    public List<Resume> Resumes { get; set; } = [];
}
