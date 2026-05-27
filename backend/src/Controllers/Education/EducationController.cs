using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/education")]
[Authorize]
public partial class EducationController : ControllerBase
{
    private readonly ILogger<EducationController> _logger;
    private readonly AppDbContext _db;
    private readonly IAiService _ai;
    private readonly IAiReadinessService _readiness;

    public EducationController(ILogger<EducationController> logger, AppDbContext db, IAiService ai, IAiReadinessService readiness)
    {
        _logger = logger;
        _db = db;
        _ai = ai;
        _readiness = readiness;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
