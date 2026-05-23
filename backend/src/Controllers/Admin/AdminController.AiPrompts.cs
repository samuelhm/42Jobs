using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-prompts")]
    public async Task<IActionResult> GetAiPrompts()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var prompts = await _db.AiPrompts.Include(p => p.Schema).OrderBy(p => p.Functionality).ToListAsync();
        return Ok(new { success = true, data = prompts.Select(p => new
        {
            p.Id, p.Functionality, p.Name, p.Description,
            p.SystemPrompt, p.UserPromptTemplate,
            p.IsActive, p.SchemaId,
            SchemaName = p.Schema?.Name,
            p.CreatedAt, p.UpdatedAt
        }) });
    }

    [HttpPut("ai-prompts/{id:int}")]
    public async Task<IActionResult> UpdateAiPrompt([FromRoute] int id, [FromBody] AiPromptDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var prompt = await _db.AiPrompts.FindAsync(id);
        if (prompt is null) return NotFound(new { error = "Prompt not found" });
        prompt.SystemPrompt = body.SystemPrompt;
        prompt.UserPromptTemplate = body.UserPromptTemplate;
        prompt.IsActive = body.IsActive;
        prompt.SchemaId = body.SchemaId;
        prompt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = prompt });
    }
}

public class AiPromptDto
{
    public string SystemPrompt { get; set; } = "";
    public string UserPromptTemplate { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int? SchemaId { get; set; }
}
