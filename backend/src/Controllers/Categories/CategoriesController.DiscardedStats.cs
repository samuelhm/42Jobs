using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{id:int}/discarded-stats")]
    public async Task<IActionResult> GetDiscardedStats([FromRoute] int id)
    {
        var userId = GetUserId();

        var follows = await _db.UserCategories.AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id);
        if (!follows)
            return NotFound(new { error = "Category not found" });

        var categoryName = await _db.Categories
            .Where(c => c.Id == id)
            .Select(c => c.Name)
            .FirstOrDefaultAsync();

        var discarded = await _db.DiscardedJobs
            .Where(d => d.CategoryName == categoryName)
            .Select(d => d.FilterReasons)
            .ToListAsync();

        int seniorOnly = 0;
        int notRelevant = 0;

        foreach (var reasonsJson in discarded)
        {
            if (string.IsNullOrEmpty(reasonsJson)) continue;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(reasonsJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("juniorFriendly", out var jf) && jf.GetString() == "no")
                    seniorOnly++;

                if (root.TryGetProperty("relevant", out var rel) && rel.GetString() == "no")
                    notRelevant++;
            }
            catch { /* malformed JSON, skip */ }
        }

        return Ok(new
        {
            success = true,
            data = new { senior_only = seniorOnly, not_relevant = notRelevant }
        });
    }
}
