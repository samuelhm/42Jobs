using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (project is null) return NotFound(new { success = false, error = "Project not found" });

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
