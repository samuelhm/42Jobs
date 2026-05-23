using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProfileController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var userId = GetUserId();
        var user = await _db.Users
            .Include(u => u.Languages)
            .Include(u => u.Certifications)
            .Include(u => u.Educations)
            .Include(u => u.Projects).ThenInclude(p => p.Keywords)
            .Include(u => u.WorkExperiences).ThenInclude(w => w.Keywords)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound(new { error = "User not found" });

        var data = new ProfileResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            LastName = user.LastName,
            Phone = user.Phone,
            Address = user.Address,
            LinkedinUrl = user.LinkedinUrl,
            WebsiteUrl = user.WebsiteUrl,
            GithubUrl = user.GithubUrl,
            Junior = user.Junior,
            Presentation = user.Presentation,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            PreferredLocation = user.PreferredLocation,
            PreferredDatePosted = user.PreferredDatePosted,
            CreatedAt = user.CreatedAt,
            Languages = user.Languages.Select(l => new LanguageDto
            {
                Id = l.Id, Name = l.Name, Level = l.Level
            }).ToList(),
            Certifications = user.Certifications.Select(c => new CertificationDto
            {
                Id = c.Id, Name = c.Name, Entity = c.Entity, DateObtained = c.DateObtained?.ToString("yyyy-MM-dd")
            }).ToList(),
            Education = user.Educations.Select(e => new EducationDto
            {
                Id = e.Id, Degree = e.Degree, Institution = e.Institution,
                StartYear = e.StartYear, EndYear = e.EndYear
            }).ToList(),
            Projects = user.Projects.Select(p => new ProjectResponseDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Type = p.Type,
                Keywords = p.Keywords.Select(k => k.Name).ToList()
            }).ToList(),
            Experiences = user.WorkExperiences.Select(w => new ExperienceResponseDto
            {
                Id = w.Id, Company = w.Company, Position = w.Position,
                StartDate = w.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = w.EndDate?.ToString("yyyy-MM-dd"),
                Description = w.Description,
                Keywords = w.Keywords.Select(k => k.Name).ToList()
            }).ToList(),
        };

        return Ok(new { success = true, data });
    }
}
