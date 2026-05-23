using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public partial class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly LinkedInApiService _linkedIn;
    private readonly GeminiService _gemini;

    public JobsController(AppDbContext db, LinkedInApiService linkedIn, GeminiService gemini)
    {
        _db = db;
        _linkedIn = linkedIn;
        _gemini = gemini;
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(sub!);
    }
}

public class UpdateJobDto
{
    public string? Title { get; set; }
}

public class UpdateJobNotesDto
{
    public string? Notes { get; set; }
}
