using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{id:int}/company-types")]
    public async Task<IActionResult> GetCompanyTypes([FromRoute] int id)
    {
        var userId = GetUserId();

        var follows = await _db.UserCategories.AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id);
        if (!follows)
            return NotFound(new { error = "Category not found" });

        var types = await _db.Jobs
            .Where(j => j.Categories.Any(c => c.Id == id))
            .GroupBy(j => j.Company != null && j.Company.CompanyType != null ? j.Company.CompanyType : "Unknown")
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        return Ok(new { success = true, data = types });
    }
}
