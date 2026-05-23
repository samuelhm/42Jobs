using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ExperiencesController
{
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
}
