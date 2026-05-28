using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("discarded-jobs")]
    public async Task<IActionResult> GetDiscardedJobs(
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0)
    {
        var query = _db.DiscardedJobs.AsQueryable();

        var total = await query.CountAsync();

        var discarded = await query
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .Skip(offset)
            .Take(Math.Min(limit, 1000))
            .Select(d => new
            {
                d.Id,
                d.ExternalId,
                d.Title,
                d.CompanyName,
                d.Location,
                d.PostedDate,
                d.Description,
                d.FilterReasons,
                d.CategoryName,
                d.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { success = true, data = discarded, total });
    }
}
