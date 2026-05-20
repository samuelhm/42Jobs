using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/certifications")]
[Authorize]
public class CertificationsController : ControllerBase
{
    private readonly ILogger<CertificationsController> _logger;
    private readonly AppDbContext _db;

    public CertificationsController(ILogger<CertificationsController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

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

    private static DateOnly? TryParseDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date)) return null;
        return DateOnly.TryParse(date, out var d) ? d : null;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
