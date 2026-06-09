using System.ComponentModel.DataAnnotations;

namespace src.Models.DTOs;

public class LanguageDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Language name is required")]
    [MaxLength(100, ErrorMessage = "Language name must be at most 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Language level is required")]
    [MaxLength(50, ErrorMessage = "Language level must be at most 50 characters")]
    public string Level { get; set; } = string.Empty;
}

public class CertificationDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Certification name is required")]
    [MaxLength(200, ErrorMessage = "Certification name must be at most 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Entity must be at most 200 characters")]
    public string? Entity { get; set; }

    public string? DateObtained { get; set; }
}

public class EducationDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Degree is required")]
    [MaxLength(200, ErrorMessage = "Degree must be at most 200 characters")]
    public string Degree { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Institution must be at most 200 characters")]
    public string? Institution { get; set; }

    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
}

public class ProjectDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Project name is required")]
    [MaxLength(300, ErrorMessage = "Project name must be at most 300 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(5000, ErrorMessage = "Description must be at most 5000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Project type is required")]
    [MaxLength(20, ErrorMessage = "Type must be at most 20 characters")]
    [RegularExpression(@"^(personal|school)$", ErrorMessage = "Type must be 'personal' or 'school'")]
    public string Type { get; set; } = string.Empty;

    public List<int>? KeywordIds { get; set; }
}

public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = [];
}

public class ExperienceDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Company is required")]
    [MaxLength(200, ErrorMessage = "Company must be at most 200 characters")]
    public string Company { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "Position must be at most 200 characters")]
    public string? Position { get; set; }

    public string? StartDate { get; set; }
    public string? EndDate { get; set; }

    [MaxLength(5000, ErrorMessage = "Description must be at most 5000 characters")]
    public string? Description { get; set; }

    public List<int>? KeywordIds { get; set; }
}

public class ExperienceResponseDto
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
    public List<string> Keywords { get; set; } = [];
}

public class ProfileResponseDto
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
    public string? PreferredLocation { get; set; }
    public string? Photo { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<LanguageDto> Languages { get; set; } = [];
    public List<CertificationDto> Certifications { get; set; } = [];
    public List<EducationDto> Education { get; set; } = [];
    public List<ProjectResponseDto> Projects { get; set; } = [];
    public List<ExperienceResponseDto> Experiences { get; set; } = [];
}

public class UpdateProfileDto
{
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(300, ErrorMessage = "Email must be at most 300 characters")]
    public string? Email { get; set; }

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

    [MaxLength(200, ErrorMessage = "Location must be at most 200 characters")]
    public string? PreferredLocation { get; set; }
}

public class KeywordResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LearningStatus { get; set; }
}

public class UpdateKeywordDto
{
    [RegularExpression(@"^(not_learned|learned_personal_project|learned_in_school)$",
        ErrorMessage = "Learning status must be: not_learned, learned_personal_project, or learned_in_school")]
    public string? LearningStatus { get; set; }
}

// ─── Category listing ──────────────────────────────────────

public class CategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public bool IsFetching { get; set; }
}

// ─── Job listing ───────────────────────────────────────────

public class JobResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? PostedDate { get; set; }
    public string? Salary { get; set; }
    public string? Benefits { get; set; }
    public string? JobType { get; set; }
    public string? ExperienceLevel { get; set; }
    public string? JobUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyType { get; set; }
    public List<string> Keywords { get; set; } = [];
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Keywords per category ─────────────────────────────────

public class CategoryKeywordDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

// ─── GitHub import ─────────────────────────────────────────
public class GithubProjectResult

{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "personal";
    public List<string> Keywords { get; set; } = [];
}

// ─── LinkedIn import ───────────────────────────────────────

public class LinkedInImportDto
{
    [Required(ErrorMessage = "Raw text is required")]
    [MaxLength(100000, ErrorMessage = "Raw text must be at most 100,000 characters")]
    public string RawText { get; set; } = string.Empty;
}

public class LinkedInExperienceParsed
{
    public string Company { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }
}

public class LinkedInEducationParsed
{
    public string Degree { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
}

public class UpdatePhotoDto
{
    [MaxLength(2_200_000, ErrorMessage = "Photo data too large")]
    public string? Photo { get; set; }
}
