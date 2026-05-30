using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("blocked-keywords")]
    public async Task<IActionResult> GetBlockedKeywords()
    {
        var blocked = await _db.BlockedKeywords
            .OrderBy(b => b.Name)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.RedirectTo,
                RedirectName = b.RedirectKeyword != null ? b.RedirectKeyword.Name : null,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = blocked });
    }

    [HttpPut("blocked-keywords/{id:int}")]
    public async Task<IActionResult> UpdateBlockedKeyword(int id, [FromBody] UpdateBlockedKeywordDto body)
    {
        var entry = await _db.BlockedKeywords.FindAsync(id);
        if (entry is null)
            return NotFound(new { error = "Blocked keyword not found" });

        if (!string.IsNullOrWhiteSpace(body.RedirectToName))
        {
            var target = await _db.Keywords
                .FirstOrDefaultAsync(k => k.Name.ToLower() == body.RedirectToName.Trim().ToLower());
            if (target is null)
                return BadRequest(new { error = $"Target keyword '{body.RedirectToName}' not found" });
            entry.RedirectTo = target.Id;
        }
        else
        {
            entry.RedirectTo = null;
        }

        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Updated" });
    }

    [HttpDelete("blocked-keywords/{id:int}")]
    public async Task<IActionResult> DeleteBlockedKeyword(int id)
    {
        var entry = await _db.BlockedKeywords.FindAsync(id);
        if (entry is null)
            return NotFound(new { error = "Blocked keyword not found" });

        _db.BlockedKeywords.Remove(entry);
        await _db.SaveChangesAsync();

        return Ok(new { success = true, message = "Deleted" });
    }
}

public class UpdateBlockedKeywordDto
{
    [MaxLength(200, ErrorMessage = "Redirect name must be at most 200 characters")]
    public string? RedirectToName { get; set; }
}
