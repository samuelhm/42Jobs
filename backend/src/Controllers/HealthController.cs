using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("/")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Root()
    {
        return Ok(new { app = "BimJobsNet", status = "running" });
    }

    [HttpGet("db-test")]
    public async Task<IActionResult> DbTest([FromServices] AppDbContext db)
    {
        try
        {
            var now = await db.Database
                .SqlQueryRaw<DateTime>("SELECT NOW()")
                .FirstOrDefaultAsync();
            return Ok(new { status = "ok", server_time = now });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }
}
