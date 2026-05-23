using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class CertificationsController
{
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var cert = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cert is null) return NotFound(new { success = false, error = "Certification not found" });

        _db.Certifications.Remove(cert);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
