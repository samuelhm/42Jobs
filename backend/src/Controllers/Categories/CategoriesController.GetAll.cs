using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool available = false)
    {
        var userId = GetUserId();

        var categories = await _db.UserCategories
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Category)
            .ThenInclude(c => c.Jobs)
            .Select(uc => new CategoryResponseDto
            {
                Id = uc.Category.Id,
                Name = uc.Category.Name,
                JobCount = available
                    ? uc.Category.Jobs.Count(j => !j.UserJobs.Any(uj => uj.UserId == userId))
                    : uc.Category.Jobs.Count,
                LastFetchedAt = uc.Category.LastFetchedAt,
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }
}
