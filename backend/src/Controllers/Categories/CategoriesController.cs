using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;
using src.Services.Jobs;

namespace src.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public partial class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly AppDbContext _db;
    private readonly IJobFetchService _fetchService;
    private readonly IAiReadinessService _readiness;

    public CategoriesController(ILogger<CategoriesController> logger, AppDbContext db, IJobFetchService fetchService, IAiReadinessService readiness)
    {
        _logger = logger;
        _db = db;
        _fetchService = fetchService;
        _readiness = readiness;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
