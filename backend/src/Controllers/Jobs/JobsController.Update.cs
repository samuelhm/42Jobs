using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class JobsController
{
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateJobDto body)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null) return NotFound(new { error = "Job not found" });

        if (body.Title is not null) job.Title = body.Title;

        job.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { job.Id, job.Title } });
    }
}
