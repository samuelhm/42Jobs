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
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(AppDbContext db, IAiService ai, ILogger<ResumesController> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
