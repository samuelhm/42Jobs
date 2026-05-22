using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrackingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var jobs = await _db.Resumes
            .Where(r => r.UserId == userId && r.Job != null)
            .Include(r => r.Job!)
            .ThenInclude(j => j.Company)
            .OrderByDescending(r => r.Job!.UpdatedAt)
            .Select(r => new
            {
                r.Job!.Id,
                r.Job.Title,
                CompanyName = r.Job.Company != null ? r.Job.Company.Name : null,
                r.Job.JobUrl
            })
            .ToListAsync();

        return Ok(new { success = true, data = jobs });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
