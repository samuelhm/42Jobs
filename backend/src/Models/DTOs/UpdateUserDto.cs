using System.ComponentModel.DataAnnotations;

namespace src.Models.DTOs;

public class UpdateUserDto
{
    [MaxLength(200, ErrorMessage = "Name must be at most 200 characters")]
    public string? Name { get; set; }

    [MaxLength(200, ErrorMessage = "Last name must be at most 200 characters")]
    public string? LastName { get; set; }

    [MaxLength(50, ErrorMessage = "Phone must be at most 50 characters")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string? Phone { get; set; }

    [MaxLength(5000, ErrorMessage = "Address must be at most 5000 characters")]
    public string? Address { get; set; }

    [Url(ErrorMessage = "Invalid LinkedIn URL")]
    [MaxLength(2000, ErrorMessage = "LinkedIn URL must be at most 2000 characters")]
    public string? LinkedinUrl { get; set; }

    [Url(ErrorMessage = "Invalid website URL")]
    [MaxLength(2000, ErrorMessage = "Website URL must be at most 2000 characters")]
    public string? WebsiteUrl { get; set; }

    [Url(ErrorMessage = "Invalid GitHub URL")]
    [MaxLength(2000, ErrorMessage = "GitHub URL must be at most 2000 characters")]
    public string? GithubUrl { get; set; }

    public bool? Junior { get; set; }

    [MaxLength(5000, ErrorMessage = "Presentation must be at most 5000 characters")]
    public string? Presentation { get; set; }

    [Url(ErrorMessage = "Invalid avatar URL")]
    [MaxLength(2000, ErrorMessage = "Avatar URL must be at most 2000 characters")]
    public string? AvatarUrl { get; set; }
}
