using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("keywords-names")]
    public async Task<IActionResult> GetKeywordNames()
    {
        var names = await _db.Keywords
            .OrderBy(k => k.Name)
            .Select(k => new { k.Name })
            .ToListAsync();

        return Ok(new { success = true, data = names });
    }
}
