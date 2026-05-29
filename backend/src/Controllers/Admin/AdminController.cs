using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;
using src.Services.Jobs;

namespace src.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public partial class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiService _ai;
    private readonly EncryptionService _encryption;
    private readonly IAiReadinessService _readiness;
    private readonly IJobFetchService _fetch;
    private readonly AdminLogService _adminLog;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext db, IAiService ai, EncryptionService encryption, IAiReadinessService readiness, IJobFetchService fetch, AdminLogService adminLog, ILogger<AdminController> logger)
    {
        _db = db;
        _ai = ai;
        _encryption = encryption;
        _readiness = readiness;
        _fetch = fetch;
        _adminLog = adminLog;
        _logger = logger;
    }
}
