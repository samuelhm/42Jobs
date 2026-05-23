using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class EducationController
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var edu = await _db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (edu is null) return NotFound(new { success = false, error = "Education entry not found" });

        _db.Educations.Remove(edu);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
