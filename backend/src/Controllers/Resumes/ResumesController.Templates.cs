using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _db.CvTemplates
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Description, t.IsActive })
            .ToListAsync();

        return Ok(new { success = true, data = templates });
    }
}
