using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpPost("{id:int}/fetch")]
    public async Task<IActionResult> Fetch([FromRoute] int id)
    {
        var userId = GetUserId();

        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound(new { error = "Category not found" });

        var isFollowing = await _db.UserCategories
            .AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id);

        if (!isFollowing)
            return BadRequest(new { error = "You must follow this category first" });

        var user = await _db.Users.FindAsync(userId);
        var location = !string.IsNullOrEmpty(user?.PreferredLocation) ? user.PreferredLocation : "Barcelona";

        var jobId = _fetchService.Enqueue(category.Id, category.Name, new FetchRequestDto
        {
            Location = location,
            Limit = 10,
            DatePosted = "past-week",
            SortBy = "recent",
        });

        if (jobId is null)
            return StatusCode(503, new { error = "Fetch queue is full, try again later" });

        return Ok(new
        {
            success = true,
            job_id = jobId.Value.ToString(),
            category_id = id,
            location,
        });
    }
}
