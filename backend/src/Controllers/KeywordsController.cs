using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/keywords")]
[Authorize]
public class KeywordsController : ControllerBase
{
    private readonly ILogger<KeywordsController> _logger;
    private readonly AppDbContext _db;

    public KeywordsController(ILogger<KeywordsController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var keywords = await _db.Keywords
            .OrderBy(k => k.Name)
            .Select(k => new KeywordResponseDto
            {
                Id = k.Id,
                Name = k.Name,
                LearningStatus = k.UserKeywords
                    .Where(uk => uk.UserId == userId)
                    .Select(uk => (string?)uk.LearningStatus)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { success = true, data = keywords });
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateKeywordDto body)
    {
        var userId = GetUserId();
        var keyword = await _db.Keywords.FindAsync(id);
        if (keyword is null) return NotFound(new { success = false, error = "Keyword not found" });

        if (body.LearningStatus is not null)
        {
            var userKw = await _db.UserKeywords
                .FirstOrDefaultAsync(uk => uk.UserId == userId && uk.KeywordId == id);

            if (userKw is null)
            {
                userKw = new UserKeyword
                {
                    UserId = userId,
                    KeywordId = id,
                    LearningStatus = body.LearningStatus
                };
                _db.UserKeywords.Add(userKw);
            }
            else
            {
                userKw.LearningStatus = body.LearningStatus;
            }

            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            success = true,
            data = new KeywordResponseDto
            {
                Id = keyword.Id,
                Name = keyword.Name,
                LearningStatus = body.LearningStatus ?? "not_learned"
            }
        });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
