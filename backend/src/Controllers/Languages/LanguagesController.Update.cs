using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class LanguagesController
{
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
}
