using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class UsersController
{
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var currentUserId = GetUserId();
        if (currentUserId != id && !User.IsInRole("Admin"))
            return Forbid();

        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(new { error = "User not found" });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted ({Email})", user.Id, user.Email);

        return NoContent();
    }
}
