using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("job-providers")]
    public async Task<IActionResult> GetJobProviders()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var providers = await _db.JobProviders.OrderBy(p => p.Portal).ThenBy(p => p.ProviderName).ToListAsync();
        return Ok(new { success = true, data = providers });
    }

    [HttpPut("job-providers/{id:int}")]
    public async Task<IActionResult> UpdateJobProvider([FromRoute] int id, [FromBody] JobProviderDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var provider = await _db.JobProviders.FindAsync(id);
        if (provider is null) return NotFound(new { error = "Provider not found" });
        provider.IsEnabled = body.IsEnabled;
        provider.IsActive = body.IsActive;
        provider.BaseUrl = body.BaseUrl;
        provider.ApiKey = body.ApiKey;
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
