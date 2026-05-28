using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("reprocess-broken-jobs")]
    public async Task<IActionResult> ReprocessBrokenJobs()
    {
        var jobIdsWithKeywords = await _db.Set<Dictionary<string, object>>("job_keywords")
            .Select(jk => EF.Property<int>(jk, "job_id"))
            .Distinct()
            .ToListAsync();

        var trackedJobIds = await _db.UserJobs
            .Select(uj => uj.JobId)
            .Distinct()
            .ToListAsync();

        var brokenJobs = await _db.Jobs
            .Where(j => !jobIdsWithKeywords.Contains(j.Id))
            .ToListAsync();

        int deleted = 0;
        int skippedTracked = 0;

        foreach (var job in brokenJobs)
        {
            if (trackedJobIds.Contains(job.Id))
            {
                skippedTracked++;
                continue;
            }

            _db.Jobs.Remove(job);
            deleted++;
        }

        if (deleted > 0)
            await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new { deleted, skipped_tracked = skippedTracked }
        });
    }
}
