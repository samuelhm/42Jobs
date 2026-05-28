using Microsoft.AspNetCore.Mvc;
using src.Models.DTOs;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch([FromRoute] Guid id, [FromBody] UpdateUserDto body)
    {
        var currentUserId = GetUserId();
        if (currentUserId != id && !User.IsInRole("Admin"))
            return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (body.Name is not null) user.Name = body.Name;
        if (body.LastName is not null) user.LastName = body.LastName;
        if (body.Phone is not null) user.Phone = body.Phone;
        if (body.Address is not null) user.Address = body.Address;
        if (body.LinkedinUrl is not null) user.LinkedinUrl = body.LinkedinUrl;
        if (body.WebsiteUrl is not null) user.WebsiteUrl = body.WebsiteUrl;
        if (body.GithubUrl is not null) user.GithubUrl = body.GithubUrl;
        if (body.Junior.HasValue) user.Junior = body.Junior.Value;
        if (body.Presentation is not null) user.Presentation = body.Presentation;
        if (body.AvatarUrl is not null) user.AvatarUrl = body.AvatarUrl;

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} profile updated", user.Id);

        var response = new UserResponseDto
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
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };

        return Ok(response);
    }
}
