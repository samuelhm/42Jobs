using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;

namespace src.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateJobDto body)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { error = "Job not found" });

        if (body.Title is not null) job.Title = body.Title;

        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { job.Id, job.Title } });
    }

    [HttpPatch("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes([FromRoute] int id, [FromBody] UpdateJobNotesDto body)
    {
        var userId = GetUserId();

        var userJob = await _db.UserJobs
            .FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == id);

        if (userJob is null)
        {
            userJob = new UserJob { UserId = userId, JobId = id };
            _db.UserJobs.Add(userJob);
        }

        userJob.Notes = body.Notes;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();

        var userJob = await _db.UserJobs
            .FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == id);

        if (userJob is not null)
        {
            _db.UserJobs.Remove(userJob);
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

public class UpdateJobDto
{
    public string? Title { get; set; }
}

public class UpdateJobNotesDto
{
    public string? Notes { get; set; }
}
