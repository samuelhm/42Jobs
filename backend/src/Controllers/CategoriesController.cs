using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly AppDbContext _db;
    private readonly JobFetchOrchestrator _fetchOrchestrator;

    public CategoriesController(ILogger<CategoriesController> logger, AppDbContext db, JobFetchOrchestrator fetchOrchestrator)
    {
        _logger = logger;
        _db = db;
        _fetchOrchestrator = fetchOrchestrator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto body)
    {
        var userId = GetUserId();

        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Name == body.Name);

        Category category;
        if (existing is not null)
        {
            category = existing;
            _logger.LogInformation("Category '{Name}' already exists, reusing id={Id}", body.Name, category.Id);
        }
        else
        {
            category = new Category { Name = body.Name };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Category '{Name}' created with id={Id}", body.Name, category.Id);
        }

        var alreadyFollowing = await _db.UserCategories
            .AnyAsync(uc => uc.UserId == userId && uc.CategoryId == category.Id);

        if (alreadyFollowing)
        {
            return Conflict(new { error = "Already following this category" });
        }

        _db.UserCategories.Add(new UserCategory
        {
            UserId = userId,
            CategoryId = category.Id,
        });

        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} started following category {CategoryId}", userId, category.Id);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pgEx
                  && pgEx.SqlState == "23505")
        {
            _logger.LogWarning("Race condition: user {UserId} already follows category {CategoryId}", userId, category.Id);
            return Conflict(new { error = "Already following this category" });
        }

        return Created($"/api/categories/{category.Id}", new
        {
            id = category.Id,
            name = category.Name,
        });
    }

    [HttpDelete("{id:int}/follow")]
    public async Task<IActionResult> Unfollow([FromRoute] int id)
    {
        var userId = GetUserId();

        var userCategory = await _db.UserCategories.FindAsync(userId, id);
        if (userCategory is null)
        {
            return NotFound(new { error = "Not following this category" });
        }

        _db.UserCategories.Remove(userCategory);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unfollowed category {CategoryId}", userId, id);

        return NoContent();
    }

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
            && DateTime.UtcNow - category.LastFetchedAt.Value < TimeSpan.FromHours(3))
        {
            return Ok(new { status = "fresh", message = "Category already fetched within the last 3 hours" });
        }

        var existingJobId = _fetchOrchestrator.Enqueue(id, category.Name, body);

        if (existingJobId is null)
        {
            return StatusCode(503, new { error = "Fetch queue is full, try again later" });
        }

        var status = _fetchOrchestrator.GetStatus(existingJobId.Value);
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

    [HttpGet("{categoryId:int}/fetch/{jobId:guid}")]
    public IActionResult GetFetchStatus([FromRoute] int categoryId, [FromRoute] Guid jobId)
    {
        var status = _fetchOrchestrator.GetStatus(jobId);
        if (status is null)
        {
            return NotFound(new { error = "Fetch job not found" });
        }

        return Ok(status);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
