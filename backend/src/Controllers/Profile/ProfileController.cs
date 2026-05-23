using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public partial class ProfileController : ControllerBase
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _db;

    public ProfileController(ILogger<ProfileController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
