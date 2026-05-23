using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpDelete("{id:int}/follow")]
    public async Task<IActionResult> Unfollow([FromRoute] int id)
    {
        var userId = GetUserId();

        var userCategory = await _db.UserCategories.FindAsync(userId, id);
        if (userCategory is null)
        {
            return NotFound(new { error = "Not following this category" });
        }

        _db.UserCategories.Remove(userCategory);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} unfollowed category {CategoryId}", userId, id);

        return NoContent();
    }
}
