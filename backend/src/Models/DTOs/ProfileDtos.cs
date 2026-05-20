namespace src.Models.DTOs;

public class LanguageDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

public class CertificationDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string? DateObtained { get; set; }
}

public class EducationDto
{
    public int? Id { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }
}

public class ProjectDto
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
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
    public string Company { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
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
    public DateTime CreatedAt { get; set; }

    public List<LanguageDto> Languages { get; set; } = [];
    public List<CertificationDto> Certifications { get; set; } = [];
    public List<EducationDto> Education { get; set; } = [];
    public List<ProjectResponseDto> Projects { get; set; } = [];
    public List<ExperienceResponseDto> Experiences { get; set; } = [];
}

public class UpdateProfileDto
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
}

public class KeywordResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LearningStatus { get; set; } = string.Empty;
}

public class UpdateKeywordDto
{
    public string? LearningStatus { get; set; }
}
