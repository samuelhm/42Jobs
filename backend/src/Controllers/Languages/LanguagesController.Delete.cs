using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class LanguagesController
{
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
}
