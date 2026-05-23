using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class ExperiencesController
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var exp = await _db.WorkExperiences.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (exp is null) return NotFound(new { success = false, error = "Experience not found" });

        _db.WorkExperiences.Remove(exp);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
