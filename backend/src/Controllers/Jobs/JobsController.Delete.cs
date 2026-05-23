using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            _db.UserJobs.Remove(userJob);
            await _db.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }
}
