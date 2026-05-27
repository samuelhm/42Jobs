using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public partial class ResumesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiService _ai;
    private readonly IAiReadinessService _readiness;
    private readonly CvGenerationTracker _cvTracker;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(AppDbContext db, IAiService ai, IAiReadinessService readiness, CvGenerationTracker cvTracker, ILogger<ResumesController> logger)
    {
        _db = db;
        _ai = ai;
        _readiness = readiness;
        _cvTracker = cvTracker;
        _logger = logger;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
