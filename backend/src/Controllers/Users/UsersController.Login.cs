using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto body)
    {
        var normalizedEmail = body.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null || user.PasswordHash is null
            || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", normalizedEmail);
            return Unauthorized(new { error = "Invalid email or password" });
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.Generate(user);
        var cookieOptions = _jwt.GetCookieOptions();

        Response.Cookies.Append(_jwt.CookieName, token, cookieOptions);

        _logger.LogInformation("User {UserId} logged in ({Email})", user.Id, user.Email);

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.Name,
            lastName = user.LastName,
            junior = user.Junior
        });
    }
}
