using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public partial class CategoriesController : ControllerBase
{
    private readonly ILogger<CategoriesController> _logger;
    private readonly AppDbContext _db;
    private readonly JobFetchOrchestrator _fetchOrchestrator;

    public CategoriesController(ILogger<CategoriesController> logger, AppDbContext db, JobFetchOrchestrator fetchOrchestrator)
    {
        _logger = logger;
        _db = db;
        _fetchOrchestrator = fetchOrchestrator;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}
