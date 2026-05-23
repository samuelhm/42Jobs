using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class JobsController
{
    [HttpPatch("{id:int}/notes")]
    public async Task<IActionResult> UpdateNotes([FromRoute] int id, [FromBody] UpdateJobNotesDto body)
    {
        var userId = GetUserId();

        var userJob = await _db.UserJobs
            .FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == id);

        if (userJob is null)
        {
            userJob = new UserJob { UserId = userId, JobId = id };
            _db.UserJobs.Add(userJob);
        }

        userJob.Notes = body.Notes;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
