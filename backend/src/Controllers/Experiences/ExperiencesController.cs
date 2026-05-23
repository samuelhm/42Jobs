using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/experiences")]
[Authorize]
public partial class ExperiencesController : ControllerBase
{
    private readonly ILogger<ExperiencesController> _logger;
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;
    private readonly OpenAIService _openAi;

    public ExperiencesController(ILogger<ExperiencesController> logger, AppDbContext db, GeminiService gemini, OpenAIService openAi)
    {
        _logger = logger;
        _db = db;
        _gemini = gemini;
        _openAi = openAi;
    }

    private async Task SyncExperienceKeywords(int expId, List<int> keywordIds)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM work_experience_keywords WHERE experience_id = {0}", expId);

        foreach (var kwId in keywordIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO work_experience_keywords (experience_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                expId, kwId);
        }
    }

    private static DateOnly? TryParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        return DateOnly.TryParse(date, out var d) ? d : null;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
