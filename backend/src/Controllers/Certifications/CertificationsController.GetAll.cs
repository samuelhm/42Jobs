using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CertificationsController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var certs = await _db.Certifications
            .Where(c => c.UserId == userId)
            .Select(c => new CertificationDto
            {
                Id = c.Id, Name = c.Name, Entity = c.Entity,
                DateObtained = c.DateObtained.HasValue ? c.DateObtained.Value.ToString("yyyy-MM-dd") : null
            })
            .ToListAsync();

        return Ok(new { success = true, data = certs });
    }
}
