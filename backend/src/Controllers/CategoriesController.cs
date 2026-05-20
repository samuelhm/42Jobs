using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly AppDbContext _db;

    public CategoriesController(ILogger<CategoriesController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
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

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
