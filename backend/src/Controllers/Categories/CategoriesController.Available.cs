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

        var available = await _db.Categories
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

        return Ok(new { success = true, data = available });
    }
}
