using System.ComponentModel.DataAnnotations;

namespace src.Models.DTOs;

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    [Url(ErrorMessage = "Invalid URL format")]
    public string? LinkedinUrl { get; set; }
    [Url(ErrorMessage = "Invalid URL format")]
    public string? WebsiteUrl { get; set; }
    [Url(ErrorMessage = "Invalid URL format")]
    public string? GithubUrl { get; set; }
    public bool? Junior { get; set; }
    public string? Presentation { get; set; }
    [Url(ErrorMessage = "Invalid URL format")]
    public string? AvatarUrl { get; set; }
}
