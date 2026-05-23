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
    private readonly GeminiService _gemini;

    public EducationController(ILogger<EducationController> logger, AppDbContext db, GeminiService gemini)
    {
        _logger = logger;
        _db = db;
        _gemini = gemini;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
