using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/admin")]
public partial class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiService _ai;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext db, IAiService ai, ILogger<AdminController> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    private IActionResult EnsureAdmin()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value;
        if (role != "Admin")
            return Forbid();
        return null!;
    }
}
