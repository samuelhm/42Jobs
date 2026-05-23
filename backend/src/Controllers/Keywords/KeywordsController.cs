using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/keywords")]
[Authorize]
public partial class KeywordsController : ControllerBase
{
    private readonly ILogger<KeywordsController> _logger;
    private readonly AppDbContext _db;

    public KeywordsController(ILogger<KeywordsController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
