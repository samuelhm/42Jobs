using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/tracking")]
[Authorize]
public partial class TrackingController : ControllerBase
{
    private readonly AppDbContext _db;

    public TrackingController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
