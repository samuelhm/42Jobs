using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class EducationController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var entries = await _db.Educations
            .Where(e => e.UserId == userId)
            .Select(e => new EducationDto
            {
                Id = e.Id, Degree = e.Degree, Institution = e.Institution,
                StartYear = e.StartYear, EndYear = e.EndYear
            })
            .ToListAsync();

        return Ok(new { success = true, data = entries });
    }
}
