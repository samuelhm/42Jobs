using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class UsersController
{
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound(new { error = "User not found" });

        return Ok(new
        {
            success = true,
            data = new
            {
                id = user.Id,
                email = user.Email,
                name = user.Name,
                last_name = user.LastName,
                role = user.Role
            }
        });
    }
}
