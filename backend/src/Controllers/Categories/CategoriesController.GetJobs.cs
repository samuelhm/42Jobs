using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{id:int}/jobs")]
    public async Task<IActionResult> GetJobs([FromRoute] int id, [FromQuery] bool showTracked = false)
    {
        var userId = GetUserId();

        var follows = await _db.UserCategories.AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id);
        if (!follows)
            return NotFound(new { error = "Category not found" });

        var user = await _db.Users.FindAsync(userId);

        var query = _db.Jobs
            .Where(j => j.Categories.Any(c => c.Id == id)
                && !j.Resumes.Any(r => r.UserId == userId));

        if (!showTracked)
            query = query.Where(j => !j.UserJobs.Any(uj => uj.UserId == userId));

        var loc = user?.PreferredLocation;
        if (!string.IsNullOrEmpty(loc))
            query = query.Where(j => j.Location != null && EF.Functions.ILike(j.Location, $"%{loc}%"));

        var jobs = await query
            .Include(j => j.Company)
            .Include(j => j.Keywords)
            .OrderByDescending(j => j.PostedDate)
            .Select(j => new JobResponseDto
            {
                Id = j.Id,
                Title = j.Title ?? string.Empty,
                Description = j.Description,
                Location = j.Location,
                PostedDate = j.PostedDate.HasValue ? j.PostedDate.Value.ToString("yyyy-MM-dd") : null,
                Salary = j.Salary,
                Benefits = j.Benefits,
                JobType = j.JobType,
                ExperienceLevel = j.ExperienceLevel,
                JobUrl = j.JobUrl,
                CompanyName = j.Company != null ? j.Company.Name : null,
                CompanyType = j.Company != null ? j.Company.CompanyType : null,
                Keywords = j.Keywords.Select(k => k.Name).ToList(),
                CreatedAt = j.CreatedAt,
            })
            .ToListAsync();

        var jobIds = jobs.Select(j => j.Id).ToList();
        var userJobs = await _db.UserJobs
            .Where(uj => uj.UserId == userId && jobIds.Contains(uj.JobId))
            .ToListAsync();

        foreach (var job in jobs)
        {
            var uj = userJobs.FirstOrDefault(u => u.JobId == job.Id);
            if (uj is not null)
                job.Notes = uj.Notes;
        }

        _ = TriggerFetchIfStale(id, user?.PreferredLocation);

        return Ok(new { success = true, data = jobs });
    }

    private async Task TriggerFetchIfStale(int categoryId, string? preferredLocation)
    {
        try
        {
            var category = await _db.Categories.FindAsync(categoryId);
            if (category is null) return;

            if (category.LastFetchedAt.HasValue && category.LastFetchedAt.Value > DateTime.UtcNow.AddHours(-12))
                return;

            category.LastFetchedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var location = !string.IsNullOrEmpty(preferredLocation) ? preferredLocation : "Barcelona";
            _fetchService.Enqueue(category.Id, category.Name, new FetchRequestDto
            {
                Location = location,
                Limit = 10,
                DatePosted = "past-24h",
                SortBy = "recent",
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger on-demand fetch for category {CategoryId}", categoryId);
        }
    }
}
