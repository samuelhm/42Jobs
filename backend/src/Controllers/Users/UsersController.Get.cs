using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class UsersController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "User not found" });
        }

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
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
        };

        return Ok(response);
    }
}
