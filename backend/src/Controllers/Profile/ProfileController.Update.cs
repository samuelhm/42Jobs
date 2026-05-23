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

        if (body.Name is not null) user.Name = body.Name;
        if (body.LastName is not null) user.LastName = body.LastName;
        if (body.Phone is not null) user.Phone = body.Phone;
        if (body.Address is not null) user.Address = body.Address;
        if (body.LinkedinUrl is not null) user.LinkedinUrl = body.LinkedinUrl;
        if (body.WebsiteUrl is not null) user.WebsiteUrl = body.WebsiteUrl;
        if (body.GithubUrl is not null) user.GithubUrl = body.GithubUrl;
        if (body.Junior.HasValue) user.Junior = body.Junior.Value;
        if (body.Presentation is not null) user.Presentation = body.Presentation;

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

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
                CreatedAt = user.CreatedAt,
            }
        });
    }
}
