using Microsoft.AspNetCore.Mvc;
using src.Data;

namespace src.Controllers;

[ApiController]
[Route("api/users")]
public partial class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly AppDbContext _db;

    public UsersController(ILogger<UsersController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }
}
