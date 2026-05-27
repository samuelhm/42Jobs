using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public partial class JobsController : ControllerBase
{
    private readonly AppDbContext _db;

    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

public class UpdateJobDto
{
    public string? Title { get; set; }
}

public class UpdateJobNotesDto
{
    public string? Notes { get; set; }
}
