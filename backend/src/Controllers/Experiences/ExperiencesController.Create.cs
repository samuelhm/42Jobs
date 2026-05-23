using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ExperiencesController
{
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
}
