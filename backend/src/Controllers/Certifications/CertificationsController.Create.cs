using Microsoft.AspNetCore.Mvc;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class CertificationsController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CertificationDto body)
    {
        var userId = GetUserId();
        var cert = new Certification
        {
            UserId = userId,
            Name = body.Name,
            Entity = body.Entity,
            DateObtained = TryParseDate(body.DateObtained)
        };
        _db.Certifications.Add(cert);
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
