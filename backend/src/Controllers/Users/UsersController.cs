using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public partial class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public UsersController(ILogger<UsersController> logger, AppDbContext db, JwtService jwt)
    {
        _logger = logger;
        _db = db;
        _jwt = jwt;
    }
}
