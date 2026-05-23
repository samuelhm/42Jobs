namespace src.Models.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GithubUrl { get; set; }
    public bool Junior { get; set; }
    public string? Presentation { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserCreateResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Junior { get; set; }
    public DateTime CreatedAt { get; set; }
}