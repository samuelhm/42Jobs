namespace src.Models.DTOs;

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GithubUrl { get; set; }
    public bool? Junior { get; set; }
    public string? Presentation { get; set; }
    public string? AvatarUrl { get; set; }
}
