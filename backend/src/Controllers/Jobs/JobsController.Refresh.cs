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
        if (string.IsNullOrEmpty(job.ExternalId))
            return BadRequest(new { error = "Job has no external ID" });

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
            var details = await _linkedIn.GetDetailsAsync(job.ExternalId, CancellationToken.None);
            if (details is not null)
            {
                job.Description = details.Description;
                job.JobType = details.JobType;
                job.ExperienceLevel = details.ExperienceLevel;
                job.Industry = details.Industry;
                job.JobFunction = details.JobFunction;
                job.Applicants = details.Applicants;
            }

            var parts = new List<string?> { job.Title, job.Benefits, job.Description };
            var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
            var (skills, _) = await _ai.ExtractKeywordsAsync(inputText);

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
        catch (Exception)
        {
            return StatusCode(500, new { error = "Job refresh failed" });
        }
    }
}
