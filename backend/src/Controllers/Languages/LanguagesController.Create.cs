using Microsoft.AspNetCore.Mvc;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class LanguagesController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LanguageDto body)
    {
        var userId = GetUserId();
        var lang = new Language { UserId = userId, Name = body.Name, Level = body.Level };
        _db.Languages.Add(lang);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new LanguageDto { Id = lang.Id, Name = lang.Name, Level = lang.Level } });
    }
}
