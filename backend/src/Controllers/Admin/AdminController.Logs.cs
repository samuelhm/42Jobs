using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? actor,
        [FromQuery] string? action,
        [FromQuery] string? payload2,
        [FromQuery] int limit = 200)
    {
        var query = _db.AdminLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(actor))
            query = query.Where(l => l.Actor == actor);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(payload2))
            query = query.Where(l => l.Payload2 != null && l.Payload2.Contains(payload2));

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(Math.Min(limit, 1000))
            .Select(l => new
            {
                l.Id,
                l.CreatedAt,
                l.Actor,
                l.Action,
                l.Payload1,
                l.Payload2,
                l.Payload3,
            })
            .AsNoTracking()
            .ToListAsync();

        var actors = await _db.AdminLogs
            .Select(l => l.Actor)
            .Distinct()
            .OrderBy(a => a)
            .AsNoTracking()
            .ToListAsync();

        var actions = await _db.AdminLogs
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .AsNoTracking()
            .ToListAsync();

        return Ok(new { success = true, data = logs, actors, actions });
    }
}
