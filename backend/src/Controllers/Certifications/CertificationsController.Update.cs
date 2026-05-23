using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CertificationsController
{
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CertificationDto body)
    {
        var userId = GetUserId();
        var cert = await _db.Certifications.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (cert is null) return NotFound(new { success = false, error = "Certification not found" });

        cert.Name = body.Name;
        cert.Entity = body.Entity;
        cert.DateObtained = TryParseDate(body.DateObtained);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new CertificationDto
            {
                Id = cert.Id, Name = cert.Name, Entity = cert.Entity,
                DateObtained = cert.DateObtained?.ToString("yyyy-MM-dd")
            }
        });
    }
}
