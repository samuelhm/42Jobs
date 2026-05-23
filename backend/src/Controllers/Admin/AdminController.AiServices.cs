using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-services")]
    public async Task<IActionResult> GetAiServices()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var services = await _db.AiServices.Include(s => s.Models).AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        return Ok(new { success = true, data = services.Select(s => new
        {
            s.Id, s.Name, s.BaseUrl, s.IsActive,
            models = s.Models.Select(m => new { m.Id, m.Name, m.IsActive }).ToList()
        }).ToList() });
    }

    [HttpPost("ai-services")]
    public async Task<IActionResult> CreateAiService([FromBody] AiServiceDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var service = new AiService { Name = body.Name, BaseUrl = body.BaseUrl, ApiKey = body.ApiKey, IsActive = body.IsActive };
        _db.AiServices.Add(service);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = service });
    }

    [HttpPut("ai-services/{id:int}")]
    public async Task<IActionResult> UpdateAiService([FromRoute] int id, [FromBody] AiServiceDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var service = await _db.AiServices.FindAsync(id);
        if (service is null) return NotFound(new { error = "Service not found" });
        service.Name = body.Name;
        service.BaseUrl = body.BaseUrl;
        service.ApiKey = body.ApiKey;
        service.IsActive = body.IsActive;
        service.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = service });
    }

    [HttpDelete("ai-services/{id:int}")]
    public async Task<IActionResult> DeleteAiService([FromRoute] int id)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var service = await _db.AiServices.FindAsync(id);
        if (service is null) return NotFound(new { error = "Service not found" });
        _db.AiServices.Remove(service);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class AiServiceDto
{
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
}
