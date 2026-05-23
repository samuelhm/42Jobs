using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpPost("{id:int}/fetch")]
    public async Task<IActionResult> FetchJobs([FromRoute] int id, [FromBody] FetchRequestDto body)
    {
        var userId = GetUserId();

        var category = await _db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound(new { error = "Category not found" });
        }

        if (!await _db.UserCategories.AnyAsync(uc => uc.UserId == userId && uc.CategoryId == id))
        {
            _db.UserCategories.Add(new UserCategory { UserId = userId, CategoryId = id });
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} auto-followed category {CategoryId}", userId, id);
        }

        if (category.LastFetchedAt is not null
            && DateTime.UtcNow - category.LastFetchedAt.Value < TimeSpan.FromHours(24))
        {
            return Ok(new { status = "fresh", message = "Category already fetched within the last 24 hours" });
        }

        var existingJobId = _fetchService.Enqueue(id, category.Name, body);

        if (existingJobId is null)
        {
            return StatusCode(503, new { error = "Fetch queue is full, try again later" });
        }

        var status = _fetchService.GetStatus(existingJobId.Value);
        if (status is not null && status.Status == "queued")
        {
            category.LastFetchedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Accepted(new FetchJobResponseDto
            {
                JobId = existingJobId.Value,
                Status = "queued",
                StatusUrl = $"/api/categories/{id}/fetch/{existingJobId.Value}",
            });
        }

        return Ok(new FetchJobResponseDto
        {
            JobId = existingJobId.Value,
            Status = status?.Status ?? "running",
            StatusUrl = $"/api/categories/{id}/fetch/{existingJobId.Value}",
        });
    }
}
