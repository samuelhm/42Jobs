using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool available = false)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        var loc = user?.PreferredLocation;
        var hasLocation = !string.IsNullOrEmpty(loc);

        var categories = await _db.UserCategories
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Category)
            .ThenInclude(c => c.Jobs)
            .Select(uc => new CategoryResponseDto
            {
                Id = uc.Category.Id,
                Name = uc.Category.Name,
                JobCount = available
                    ? uc.Category.Jobs.Count(j =>
                        !j.UserJobs.Any(uj => uj.UserId == userId)
                        && (!hasLocation || (j.Location != null && EF.Functions.ILike(j.Location, $"%{loc}%"))))
                    : uc.Category.Jobs.Count(j =>
                        !hasLocation || (j.Location != null && EF.Functions.ILike(j.Location, $"%{loc}%"))),
                LastFetchedAt = uc.Category.LastFetchedAt,
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        foreach (var c in categories)
            c.IsFetching = _fetchService.IsCategoryFetching(c.Id);

        return Ok(new { success = true, data = categories });
    }
}
