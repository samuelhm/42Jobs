using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var categories = await _db.UserCategories
            .Where(uc => uc.UserId == userId)
            .Include(uc => uc.Category)
            .ThenInclude(c => c.Jobs)
            .Select(uc => new CategoryResponseDto
            {
                Id = uc.Category.Id,
                Name = uc.Category.Name,
                JobCount = uc.Category.Jobs.Count,
                LastFetchedAt = uc.Category.LastFetchedAt,
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }
}
