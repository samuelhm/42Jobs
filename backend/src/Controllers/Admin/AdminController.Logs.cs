using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        return Ok(new { success = true, data = new[] { new { message = "Log viewer coming soon" } } });
    }
}
