using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/experiences")]
[Authorize]
public class ExperiencesController : ControllerBase
{
    private readonly ILogger<ExperiencesController> _logger;
    private readonly AppDbContext _db;

    public ExperiencesController(ILogger<ExperiencesController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var experiences = await _db.WorkExperiences
            .Where(w => w.UserId == userId)
            .Include(w => w.Keywords)
            .Select(w => new ExperienceResponseDto
            {
                Id = w.Id, Company = w.Company, Position = w.Position,
                StartDate = w.StartDate.HasValue ? w.StartDate.Value.ToString("yyyy-MM-dd") : null,
                EndDate = w.EndDate.HasValue ? w.EndDate.Value.ToString("yyyy-MM-dd") : null,
                Description = w.Description,
                Keywords = w.Keywords.Select(k => k.Name).ToList()
            })
            .ToListAsync();

        return Ok(new { success = true, data = experiences });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ExperienceDto body)
    {
        var userId = GetUserId();
        var exp = new WorkExperience
        {
            UserId = userId,
            Company = body.Company,
            Position = body.Position,
            StartDate = TryParseDate(body.StartDate),
            EndDate = TryParseDate(body.EndDate),
            Description = body.Description
        };
        _db.WorkExperiences.Add(exp);
        await _db.SaveChangesAsync();

        if (body.KeywordIds is { Count: > 0 })
        {
            await SyncExperienceKeywords(exp.Id, body.KeywordIds);
        }

        var keywords = body.KeywordIds is not null
            ? await _db.Keywords.Where(k => body.KeywordIds.Contains(k.Id)).Select(k => k.Name).ToListAsync()
            : [];

        return Ok(new
        {
            success = true,
            data = new ExperienceResponseDto
            {
                Id = exp.Id, Company = exp.Company, Position = exp.Position,
                StartDate = exp.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = exp.EndDate?.ToString("yyyy-MM-dd"),
                Description = exp.Description, Keywords = keywords
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ExperienceDto body)
    {
        var userId = GetUserId();
        var exp = await _db.WorkExperiences.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (exp is null) return NotFound(new { success = false, error = "Experience not found" });

        exp.Company = body.Company;
        exp.Position = body.Position;
        exp.StartDate = TryParseDate(body.StartDate);
        exp.EndDate = TryParseDate(body.EndDate);
        exp.Description = body.Description;
        await _db.SaveChangesAsync();

        if (body.KeywordIds is not null)
        {
            await SyncExperienceKeywords(exp.Id, body.KeywordIds);
        }

        var keywords = body.KeywordIds is not null
            ? await _db.Keywords.Where(k => body.KeywordIds.Contains(k.Id)).Select(k => k.Name).ToListAsync()
            : await _db.Entry(exp).Collection(w => w.Keywords).Query().Select(k => k.Name).ToListAsync();

        return Ok(new
        {
            success = true,
            data = new ExperienceResponseDto
            {
                Id = exp.Id, Company = exp.Company, Position = exp.Position,
                StartDate = exp.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = exp.EndDate?.ToString("yyyy-MM-dd"),
                Description = exp.Description, Keywords = keywords
            }
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var exp = await _db.WorkExperiences.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (exp is null) return NotFound(new { success = false, error = "Experience not found" });

        _db.WorkExperiences.Remove(exp);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private async Task SyncExperienceKeywords(int expId, List<int> keywordIds)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM work_experience_keywords WHERE experience_id = {0}", expId);

        foreach (var kwId in keywordIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO work_experience_keywords (experience_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                expId, kwId);
        }
    }

    private static DateOnly? TryParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        return DateOnly.TryParse(date, out var d) ? d : null;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
