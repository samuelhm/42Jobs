using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProfileController
{
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileDto body)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { error = "User not found" });

        if (body.Email is not null)
        {
            var email = body.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { error = "Email cannot be empty" });
            if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                return BadRequest(new { error = "Invalid email format" });
            if (!email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var exists = await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId);
                if (exists)
                    return Conflict(new { error = "Email already in use" });
                user.Email = email;
            }
        }

        var oldLocation = user.PreferredLocation;

        if (body.Name is not null) user.Name = body.Name;
        if (body.LastName is not null) user.LastName = body.LastName;
        if (body.Phone is not null) user.Phone = body.Phone;
        if (body.Address is not null) user.Address = body.Address;
        if (body.LinkedinUrl is not null) user.LinkedinUrl = body.LinkedinUrl;
        if (body.WebsiteUrl is not null) user.WebsiteUrl = body.WebsiteUrl;
        if (body.GithubUrl is not null) user.GithubUrl = body.GithubUrl;
        if (body.Junior.HasValue) user.Junior = body.Junior.Value;
        if (body.Presentation is not null) user.Presentation = body.Presentation;
        if (body.PreferredLocation is not null) user.PreferredLocation = body.PreferredLocation;

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var newLocation = user.PreferredLocation;
        int categoriesFetched = 0;

        if (body.PreferredLocation is not null
            && !string.IsNullOrEmpty(newLocation)
            && !string.Equals(oldLocation, newLocation, StringComparison.OrdinalIgnoreCase))
        {
            var followedCategories = await _db.UserCategories
                .Where(uc => uc.UserId == userId)
                .Include(uc => uc.Category)
                .ToListAsync();

            foreach (var uc in followedCategories)
            {
                _fetchService.Enqueue(uc.CategoryId, uc.Category.Name, new FetchRequestDto
                {
                    Location = newLocation,
                    Limit = 10,
                    DatePosted = "past-week",
                    SortBy = "recent",
                });
                categoriesFetched++;
            }
        }

        return Ok(new
        {
            success = true,
            data = new ProfileResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                LastName = user.LastName,
                Phone = user.Phone,
                Address = user.Address,
                LinkedinUrl = user.LinkedinUrl,
                WebsiteUrl = user.WebsiteUrl,
                GithubUrl = user.GithubUrl,
                Junior = user.Junior,
                Presentation = user.Presentation,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                PreferredLocation = user.PreferredLocation,
                CreatedAt = user.CreatedAt,
            },
            fetch_triggered = categoriesFetched > 0,
            categories_fetched = categoriesFetched,
            location = newLocation,
        });
    }
}
