using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class JobsController
{
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
}
