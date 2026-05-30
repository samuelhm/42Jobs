using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-services")]
    public async Task<IActionResult> GetAiServices()
    {
        var services = await _db.AiServices.Include(s => s.Models).AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        return Ok(new { success = true, data = services.Select(s => new
        {
            s.Id, s.Name, s.IsActive, s.IsFreeTier,
            models = s.Models.Select(m => new { m.Id, m.Name, m.IsActive }).ToList()
        }).ToList() });
    }

    [HttpPost("ai-services")]
    [EnableRateLimiting("admin_write")]
    public async Task<IActionResult> CreateAiService([FromBody] AiServiceDto body)
    {
        var isFreeTier = body.Name == "DeepSeek" ? false : body.IsFreeTier;
        var service = new AiService { Name = body.Name, ApiKey = _encryption.Encrypt(body.ApiKey), IsActive = body.IsActive, IsFreeTier = isFreeTier };
        _db.AiServices.Add(service);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = service });
    }

    [HttpPut("ai-services/{id:int}")]
    [EnableRateLimiting("admin_write")]
    public async Task<IActionResult> UpdateAiService([FromRoute] int id, [FromBody] AiServiceDto body)
    {
        var service = await _db.AiServices.FindAsync(id);
        if (service is null) return NotFound(new { error = "Service not found" });
        service.Name = body.Name;
        service.ApiKey = _encryption.Encrypt(body.ApiKey);
        service.IsActive = body.IsActive;
        service.IsFreeTier = service.Name == "DeepSeek" ? false : body.IsFreeTier;
        service.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = service });
    }

    [HttpDelete("ai-services/{id:int}")]
    public async Task<IActionResult> DeleteAiService([FromRoute] int id)
    {
        var service = await _db.AiServices.FindAsync(id);
        if (service is null) return NotFound(new { error = "Service not found" });
        _db.AiServices.Remove(service);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class AiServiceDto
{
    [Required(ErrorMessage = "Service name is required")]
    [MaxLength(50, ErrorMessage = "Service name must be at most 50 characters")]
    public string Name { get; set; } = "";

    [MaxLength(500, ErrorMessage = "API key must be at most 500 characters")]
    public string? ApiKey { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFreeTier { get; set; }
}
