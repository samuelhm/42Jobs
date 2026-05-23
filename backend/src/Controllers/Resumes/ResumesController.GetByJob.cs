using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpGet("job/{jobId:int}")]
    public async Task<IActionResult> GetByJob([FromRoute] int jobId)
    {
        var userId = GetUserId();
        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == jobId);
        if (resume is null) return NotFound(new { error = "No CV generated for this job" });

        return Ok(new { success = true, id = resume.Id, html = resume.CvData, model = resume.Model });
    }
}
