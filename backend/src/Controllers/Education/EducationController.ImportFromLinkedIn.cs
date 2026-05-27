using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class EducationController
{
    [HttpPost("import-linkedin")]
    public async Task<IActionResult> ImportFromLinkedIn([FromBody] LinkedInImportDto body)
    {
        if (string.IsNullOrWhiteSpace(body.RawText))
            return BadRequest(new { error = "Raw text is required" });

        var parseErrors = await _readiness.CheckAsync("parse_education");
        if (parseErrors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", parseErrors) });

        var (parsed, error) = await _ai.ParseLinkedInEducationAsync(body.RawText);
        if (error is not null)
            return Ok(new { success = false, error, imported = 0 });

        if (parsed.Count == 0)
            return Ok(new { success = true, imported = 0 });

        var userId = GetUserId();
        int imported = 0;

        foreach (var edu in parsed)
        {
            _db.Educations.Add(new Education
            {
                UserId = userId,
                Degree = edu.Degree,
                Institution = edu.Institution,
                StartYear = edu.StartYear,
                EndYear = edu.EndYear
            });
            imported++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, imported });
    }
}
