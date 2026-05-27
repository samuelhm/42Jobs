using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
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

    public CategoriesController(ILogger<CategoriesController> logger, AppDbContext db, IJobFetchService fetchService)
    {
        _logger = logger;
        _db = db;
        _fetchService = fetchService;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
