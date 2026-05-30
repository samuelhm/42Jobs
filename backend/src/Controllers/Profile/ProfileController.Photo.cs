using Microsoft.AspNetCore.Mvc;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProfileController
{
    [HttpPost("photo")]
    public async Task<IActionResult> UpdatePhoto([FromBody] UpdatePhotoDto body)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound(new { error = "User not found" });

        if (body.Photo is not null)
        {
            if (!body.Photo.StartsWith("data:image/") && !body.Photo.StartsWith("https://"))
                return BadRequest(new { error = "Photo must be a data URL (data:image/...) or a secure URL (https://...)" });

            if (body.Photo.Length > 2_097_152)
                return BadRequest(new { error = "Photo too large (max 2MB)" });

            user.Photo = body.Photo;
        }
        else
        {
            user.Photo = null;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
