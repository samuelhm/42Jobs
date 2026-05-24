using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("job-providers")]
    public async Task<IActionResult> GetJobProviders()
    {
        var providers = await _db.JobProviders.OrderBy(p => p.Portal).ThenBy(p => p.ProviderName).ToListAsync();
        return Ok(new { success = true, data = providers.Select(p => new
        {
            p.Id, p.Portal, p.ProviderName, p.IsEnabled, p.IsActive,
            p.BaseUrl, p.Config, p.CreatedAt, p.UpdatedAt,
            api_key_set = !string.IsNullOrEmpty(p.ApiKey)
        }).ToList() });
    }

    [HttpPut("job-providers/{id:int}")]
    public async Task<IActionResult> UpdateJobProvider([FromRoute] int id, [FromBody] JobProviderDto body)
    {
        var provider = await _db.JobProviders.FindAsync(id);
        if (provider is null) return NotFound(new { error = "Provider not found" });
        provider.IsEnabled = body.IsEnabled;
        provider.IsActive = body.IsActive;
        provider.BaseUrl = body.BaseUrl;
        provider.ApiKey = _encryption.Encrypt(body.ApiKey);
        provider.Config = body.Config;
        provider.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = provider });
    }
}

public class JobProviderDto
{
    public bool IsEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Config { get; set; }
}
