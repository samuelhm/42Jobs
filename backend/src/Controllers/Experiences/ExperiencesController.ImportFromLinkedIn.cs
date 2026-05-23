using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ExperiencesController
{
    [HttpPost("import-linkedin")]
    public async Task<IActionResult> ImportFromLinkedIn([FromBody] LinkedInImportDto body)
    {
        if (string.IsNullOrWhiteSpace(body.RawText))
            return BadRequest(new { error = "Raw text is required" });

        var (parsed, error) = await _openAi.ParseExperienceAsync(body.RawText);
        if (error is not null)
            return Ok(new { success = false, error, imported = 0 });

        if (parsed.Count == 0)
            return Ok(new { success = true, imported = 0 });

        var userId = GetUserId();
        int imported = 0;

        foreach (var exp in parsed)
        {
            _db.WorkExperiences.Add(new WorkExperience
            {
                UserId = userId,
                Company = exp.Company,
                Position = exp.Position,
                StartDate = TryParseDate(exp.StartDate),
                EndDate = TryParseDate(exp.EndDate),
                Description = exp.Description
            });
            imported++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, imported });
    }
}
