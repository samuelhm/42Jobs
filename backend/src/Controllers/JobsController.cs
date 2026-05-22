using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LinkedInApiService _linkedIn;
    private readonly GeminiService _gemini;

    public JobsController(AppDbContext db, LinkedInApiService linkedIn, GeminiService gemini)
    {
        _db = db;
        _linkedIn = linkedIn;
        _gemini = gemini;
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

    [HttpPatch("{id:int}/refresh")]
    public async Task<IActionResult> Refresh([FromRoute] int id)
    {
        var job = await _db.Jobs
            .Include(j => j.Keywords)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound(new { error = "Job not found" });
        if (string.IsNullOrEmpty(job.LinkedinId))
            return BadRequest(new { error = "Job has no LinkedIn ID" });

        var neverRefreshed = Math.Abs((job.UpdatedAt - job.CreatedAt).TotalSeconds) < 2;
        if (!neverRefreshed
            && DateTime.UtcNow - job.UpdatedAt < TimeSpan.FromHours(4))
        {
            var remaining = TimeSpan.FromHours(4) - (DateTime.UtcNow - job.UpdatedAt);
            return Ok(new
            {
                status = "rate-limited",
                message = $"Refresh available in {remaining.Hours}h {remaining.Minutes}m",
                remaining_seconds = (int)remaining.TotalSeconds
            });
        }

        try
        {
            var details = await _linkedIn.GetJobDetailsAsync(job.LinkedinId);
            if (details is not null)
            {
                var d = details.Value;
                if (d.TryGetProperty("description", out var desc)) job.Description = desc.GetString();
                if (d.TryGetProperty("jobType", out var jt)) job.JobType = jt.GetString();
                if (d.TryGetProperty("experienceLevel", out var el)) job.ExperienceLevel = el.GetString();
                if (d.TryGetProperty("industry", out var ind)) job.Industry = ind.GetString();
                if (d.TryGetProperty("jobFunction", out var jf)) job.JobFunction = jf.GetString();
                if (d.TryGetProperty("applicants", out var app)) job.Applicants = app.GetString();
            }

            var parts = new List<string?> { job.Title, job.Benefits, job.Description };
            var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            var (skills, _) = await _gemini.ExtractKeywordsAsync(inputText);

            var existingKws = await _db.Set<Dictionary<string, object>>("job_keywords")
                .Where(jk => EF.Property<int>(jk, "job_id") == job.Id)
                .ToListAsync();
            _db.Set<Dictionary<string, object>>("job_keywords").RemoveRange(existingKws);
            await _db.SaveChangesAsync();

            foreach (var rawName in skills)
            {
                var name = rawName.Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(name)) continue;

                var kw = await _db.Keywords.FirstOrDefaultAsync(k => k.Name == name);
                if (kw is null)
                {
                    kw = new Keyword { Name = name };
                    _db.Keywords.Add(kw);
                    await _db.SaveChangesAsync();
                }

                await _db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO job_keywords (job_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                    job.Id, kw.Id);
            }

            job.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var keywords = await _db.Entry(job).Collection(j => j.Keywords).Query()
                .Select(k => k.Name).ToListAsync();

            return Ok(new { success = true, data = new { job.Id, job.Title, job.Description, keywords } });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
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
