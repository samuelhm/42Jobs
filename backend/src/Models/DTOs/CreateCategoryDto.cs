using System.ComponentModel.DataAnnotations;

namespace src.Models.DTOs;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(100, ErrorMessage = "Name must be at most 100 characters")]
    public string Name { get; set; } = string.Empty;
}
