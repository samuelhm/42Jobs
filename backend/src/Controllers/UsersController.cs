using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        [FromRoute] Guid id,
        [FromServices] AppDbContext db)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserDto body,
        [FromServices] AppDbContext db)
    {
        var emailExists = await db.Users.AnyAsync(u => u.Email == body.Email);
        if (emailExists)
            return Conflict(new { error = "Email already registered" });

        var user = new User
        {
            Email = body.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

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
            UpdatedAt = user.UpdatedAt
        };

        return Created($"/api/users/{user.Id}", response);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(
        [FromRoute] Guid id,
        [FromBody] UpdateUserDto body,
        [FromServices] AppDbContext db)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] AppDbContext db)
    {
        throw new NotImplementedException();
    }
}
