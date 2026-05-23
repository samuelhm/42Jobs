using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var userId = GetUserId();
        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resume is null) return NotFound();

        return Ok(new { success = true, id = resume.Id, html = resume.CvData, model = resume.Model });
    }
}
