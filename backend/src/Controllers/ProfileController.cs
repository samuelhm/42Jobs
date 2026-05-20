using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _db;

    public ProfileController(ILogger<ProfileController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

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

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileDto body)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { error = "User not found" });

        if (body.Name is not null) user.Name = body.Name;
        if (body.LastName is not null) user.LastName = body.LastName;
        if (body.Phone is not null) user.Phone = body.Phone;
        if (body.Address is not null) user.Address = body.Address;
        if (body.LinkedinUrl is not null) user.LinkedinUrl = body.LinkedinUrl;
        if (body.WebsiteUrl is not null) user.WebsiteUrl = body.WebsiteUrl;
        if (body.GithubUrl is not null) user.GithubUrl = body.GithubUrl;
        if (body.Junior.HasValue) user.Junior = body.Junior.Value;
        if (body.Presentation is not null) user.Presentation = body.Presentation;

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new ProfileResponseDto
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
                CreatedAt = user.CreatedAt,
            }
        });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
