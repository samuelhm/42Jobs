using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("queue-stats")]
    public IActionResult GetQueueStats()
    {
        var stats = _fetch.GetQueueStats();
        return Ok(new { success = true, data = stats });
    }
}
