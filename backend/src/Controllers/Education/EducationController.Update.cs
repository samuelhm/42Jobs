using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class EducationController
{
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EducationDto body)
    {
        var userId = GetUserId();
        var edu = await _db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (edu is null) return NotFound(new { success = false, error = "Education entry not found" });

        edu.Degree = body.Degree;
        edu.Institution = body.Institution;
        edu.StartYear = body.StartYear;
        edu.EndYear = body.EndYear;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new EducationDto
            {
                Id = edu.Id, Degree = edu.Degree, Institution = edu.Institution,
                StartYear = edu.StartYear, EndYear = edu.EndYear
            }
        });
    }
}
