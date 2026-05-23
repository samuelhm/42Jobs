using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;
using src.Services.Jobs.Providers;

namespace src.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public partial class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJobProvider _linkedIn;
    private readonly IAiService _ai;

    public JobsController(AppDbContext db, IJobProvider linkedIn, IAiService ai)
    {
        _db = db;
        _linkedIn = linkedIn;
        _ai = ai;
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
