using System.ComponentModel.DataAnnotations;

namespace src.Models.DTOs;

public class CreateUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [MaxLength(128, ErrorMessage = "Password must be at most 128 characters")]
    public string Password { get; set; } = string.Empty;
}
