using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class LanguagesController
{
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
}
