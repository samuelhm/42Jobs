using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Create([FromBody] CreateUserDto body)
    {
        _logger.LogInformation("Registration attempt for email {Email}", body.Email);

        body.Email = body.Email.Trim().ToLowerInvariant();

        var emailExists = await _db.Users.AnyAsync(u => u.Email == body.Email);
        if (emailExists)
        {
            _logger.LogWarning("Registration failed: email {Email} already in use", body.Email);
            return Conflict(new { error = "Email already registered" });
        }

        var user = new User
        {
            Email = body.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(body.Password),
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("User {UserId} registered successfully ({Email})", user.Id, user.Email);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pgEx
                  && pgEx.SqlState == "23505")
        {
            _logger.LogWarning("Race condition: email {Email} already taken during insert", body.Email);
            return Conflict(new { error = "Email already registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving user {Email}", body.Email);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }

        var response = new UserCreateResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Junior = user.Junior,
            CreatedAt = user.CreatedAt,
        };

        return Created($"/api/users/{user.Id}", response);
    }
}
