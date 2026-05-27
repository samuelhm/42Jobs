using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("discarded-jobs")]
    public async Task<IActionResult> GetDiscardedJobs()
    {
        var discarded = await _db.DiscardedJobs
            .OrderByDescending(d => d.CreatedAt)
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
                d.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { success = true, data = discarded });
    }
}
