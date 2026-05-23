using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class HealthController
{
    [HttpGet]
    public IActionResult Root()
    {
        return Ok(new { app = "42jobs", status = "running" });
    }
}
