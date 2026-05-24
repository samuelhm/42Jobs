using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ProfileController
{
    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var user = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.PreferredLocation,
                u.PreferredDatePosted
            })
            .FirstOrDefaultAsync();

        if (user is null) return NotFound(new { error = "User not found" });

        return Ok(new
        {
            success = true,
            data = new
            {
                preferred_location = user.PreferredLocation,
                preferred_date_posted = user.PreferredDatePosted
            }
        });
    }
}
