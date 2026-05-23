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

    public AdminController(AppDbContext db, IAiService ai)
    {
        _db = db;
        _ai = ai;
    }
}
