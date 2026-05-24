using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public partial class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiService _ai;
    private readonly EncryptionService _encryption;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext db, IAiService ai, EncryptionService encryption, ILogger<AdminController> logger)
    {
        _db = db;
        _ai = ai;
        _encryption = encryption;
        _logger = logger;
    }
}
