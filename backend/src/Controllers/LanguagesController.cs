using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/languages")]
[Authorize]
public class LanguagesController : ControllerBase
{
    private readonly ILogger<LanguagesController> _logger;
    private readonly AppDbContext _db;

    public LanguagesController(ILogger<LanguagesController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var languages = await _db.Languages
            .Where(l => l.UserId == userId)
            .Select(l => new LanguageDto { Id = l.Id, Name = l.Name, Level = l.Level })
            .ToListAsync();

        return Ok(new { success = true, data = languages });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LanguageDto body)
    {
        var userId = GetUserId();
        var lang = new Language { UserId = userId, Name = body.Name, Level = body.Level };
        _db.Languages.Add(lang);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new LanguageDto { Id = lang.Id, Name = lang.Name, Level = lang.Level } });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] LanguageDto body)
    {
        var userId = GetUserId();
        var lang = await _db.Languages.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (lang is null) return NotFound(new { success = false, error = "Language not found" });

        lang.Name = body.Name;
        lang.Level = body.Level;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new LanguageDto { Id = lang.Id, Name = lang.Name, Level = lang.Level } });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var lang = await _db.Languages.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (lang is null) return NotFound(new { success = false, error = "Language not found" });

        _db.Languages.Remove(lang);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
