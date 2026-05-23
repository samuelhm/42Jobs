using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/admin")]
public partial class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;

    public AdminController(AppDbContext db, GeminiService gemini)
    {
        _db = db;
        _gemini = gemini;
    }
}
