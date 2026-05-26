using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class TrackingController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var jobs = await _db.UserJobs
            .Where(uj => uj.UserId == userId && uj.Status != "oculto")
            .Include(uj => uj.Job)
            .ThenInclude(j => j.Company)
            .Include(uj => uj.Job)
            .ThenInclude(j => j.Keywords)
            .Include(uj => uj.Job)
            .ThenInclude(j => j.Categories)
            .OrderByDescending(uj => uj.StatusUpdatedAt)
            .Select(uj => new
            {
                job_id = uj.Job.Id,
                uj.Job.Title,
                uj.Job.Description,
                uj.Job.Location,
                posted_date = uj.Job.PostedDate.HasValue ? uj.Job.PostedDate.Value.ToString("yyyy-MM-dd") : null,
                uj.Job.Salary,
                uj.Job.Benefits,
                uj.Job.JobType,
                uj.Job.ExperienceLevel,
                uj.Job.JobUrl,
                company_name = uj.Job.Company != null ? uj.Job.Company.Name : null,
                company_type = uj.Job.Company != null ? uj.Job.Company.CompanyType : null,
                keywords = uj.Job.Keywords.Select(k => k.Name).ToList(),
                categories = uj.Job.Categories.Select(c => new { c.Id, c.Name }).ToList(),
                uj.Status,
                status_updated_at = uj.StatusUpdatedAt,
                uj.Notes,
                uj.SavedAt,
            })
            .ToListAsync();

        return Ok(new { success = true, data = jobs });
    }
}
