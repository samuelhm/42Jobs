using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services.Jobs;

namespace src.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public partial class ProfileController : ControllerBase
{
    private readonly ILogger<ProfileController> _logger;
    private readonly AppDbContext _db;
    private readonly IJobFetchService _fetchService;

    public ProfileController(ILogger<ProfileController> logger, AppDbContext db, IJobFetchService fetchService)
    {
        _logger = logger;
        _db = db;
        _fetchService = fetchService;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
