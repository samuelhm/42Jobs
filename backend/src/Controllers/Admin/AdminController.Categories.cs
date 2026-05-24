using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.LastFetchedAt,
                JobCount = c.Jobs.Count,
                SubscriberCount = c.UserCategories.Count,
            })
            .ToListAsync();

        return Ok(new { success = true, data = categories });
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory([FromRoute] int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null)
            return NotFound(new { error = "Category not found" });

        var safeJobs = await _db.Jobs
            .Where(j => j.Categories.Any(c => c.Id == id))
            .Where(j => !j.UserJobs.Any())
            .Where(j => !j.Categories.Any(c => c.Id != id))
            .ToListAsync();

        if (safeJobs.Count > 0)
            _db.Jobs.RemoveRange(safeJobs);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
