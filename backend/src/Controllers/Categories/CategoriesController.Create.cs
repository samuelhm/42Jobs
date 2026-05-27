using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto body)
    {
        var userId = GetUserId();
        var normalizedInput = NormalizeName(body.Name);

        var allCategories = await _db.Categories.ToListAsync();
        var existing = allCategories.FirstOrDefault(c => NormalizeName(c.Name) == normalizedInput);

        Category category;
        List<string> readinessErrors = [];
        if (existing is not null)
        {
            category = existing;
            _logger.LogInformation("Category '{Name}' normalizes to existing '{Existing}', reusing id={Id}", body.Name, existing.Name, category.Id);
        }
        else
        {
            category = new Category { Name = body.Name };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Category '{Name}' created with id={Id}", body.Name, category.Id);

            var fetchErrors = new List<string>();
            foreach (var fn in new[] { "filter_jobs", "extract_keywords" })
                fetchErrors.AddRange(await _readiness.CheckAsync(fn));
            readinessErrors = fetchErrors;
            if (readinessErrors.Count > 0)
            {
                _logger.LogWarning("Category {Id} created but fetch skipped: {Errors}", category.Id, string.Join("; ", readinessErrors));
            }
            else
            {
                _fetchService.Enqueue(category.Id, category.Name, new FetchRequestDto
                {
                    Location = "Barcelona",
                    Limit = 10,
                    DatePosted = "past-week",
                    SortBy = "recent",
                });

                category.LastFetchedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
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
            warnings = readinessErrors.Count > 0 ? readinessErrors : null,
        });
    }

    private static string NormalizeName(string name)
    {
        return Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]", "");
    }
}
