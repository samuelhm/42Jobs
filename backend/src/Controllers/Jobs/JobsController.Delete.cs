using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class JobsController
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();

        var userJob = await _db.UserJobs
            .FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == id);

        if (userJob is not null)
        {
            userJob.Status = "oculto";
            userJob.StatusUpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.UserJobs.Add(new UserJob { UserId = userId, JobId = id, Status = "oculto" });
        }

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
