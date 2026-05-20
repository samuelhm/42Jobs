namespace src.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GithubUrl { get; set; }
    public bool Junior { get; set; } = true;
    public string? Presentation { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Language> Languages { get; set; } = [];
    public List<Certification> Certifications { get; set; } = [];
    public List<Education> Educations { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
    public List<WorkExperience> WorkExperiences { get; set; } = [];
    public List<UserProvider> UserProviders { get; set; } = [];
    public List<Resume> Resumes { get; set; } = [];
    public List<UserCategory> UserCategories { get; set; } = [];
    public List<UserJob> UserJobs { get; set; } = [];
}
