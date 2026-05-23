using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{id:int}/keywords")]
    public async Task<IActionResult> GetKeywords([FromRoute] int id)
    {
        var userId = GetUserId();

        var follows = await _db.UserCategories.AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id);
        if (!follows)
            return NotFound(new { error = "Category not found" });

        var keywords = await _db.Keywords
            .Where(k => k.Jobs.Any(j => j.Categories.Any(c => c.Id == id)))
            .Select(k => new CategoryKeywordDto
            {
                Name = k.Name,
                Count = k.Jobs.Count(j => j.Categories.Any(c => c.Id == id)),
            })
            .OrderByDescending(k => k.Count)
            .Take(25)
            .ToListAsync();

        return Ok(new { success = true, data = keywords });
    }
}
