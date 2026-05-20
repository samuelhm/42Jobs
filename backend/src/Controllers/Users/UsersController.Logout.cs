using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(_jwt.CookieName);
        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logged out" });
    }
}
