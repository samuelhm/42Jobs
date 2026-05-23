using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/languages")]
[Authorize]
public partial class LanguagesController : ControllerBase
{
    private readonly ILogger<LanguagesController> _logger;
    private readonly AppDbContext _db;

    public LanguagesController(ILogger<LanguagesController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
