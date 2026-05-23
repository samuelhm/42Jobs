using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/certifications")]
[Authorize]
public partial class CertificationsController : ControllerBase
{
    private readonly ILogger<CertificationsController> _logger;
    private readonly AppDbContext _db;

    public CertificationsController(ILogger<CertificationsController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private static DateOnly? TryParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        return DateOnly.TryParse(date, out var d) ? d : null;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
