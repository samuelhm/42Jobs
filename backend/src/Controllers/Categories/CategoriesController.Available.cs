using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var userId = GetUserId();

        var subscribedIds = await _db.UserCategories
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CategoryId)
            .ToListAsync();

        var allAvailable = await _db.Categories
            .Where(c => !subscribedIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.LastFetchedAt,
                JobCount = c.Jobs.Count,
            })
            .ToListAsync();

        var deduped = allAvailable
            .GroupBy(c => NormalizeName(c.Name))
            .Select(g => g.OrderByDescending(c => c.JobCount).First())
            .OrderBy(c => c.Name)
            .ToList();

        return Ok(new { success = true, data = deduped });
    }
}
