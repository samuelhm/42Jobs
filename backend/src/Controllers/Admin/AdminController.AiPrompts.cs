using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-prompts")]
    public async Task<IActionResult> GetAiPrompts()
    {
        var prompts = await _db.AiPrompts.Include(p => p.DefaultModel).ThenInclude(m => m!.AiService).OrderBy(p => p.Functionality).ToListAsync();
        return Ok(new { success = true, data = prompts.Select(p => new
        {
            p.Id, p.Functionality, p.Name, p.Description,
            p.SystemPrompt, p.UserPromptTemplate,
            p.IsActive,
            p.DefaultModelId,
            default_model_name = p.DefaultModel?.Name,
            default_model_service = p.DefaultModel?.AiService.Name,
            p.CreatedAt, p.UpdatedAt
        }) });
    }

    [HttpPut("ai-prompts/{id:int}")]
    public async Task<IActionResult> UpdateAiPrompt([FromRoute] int id, [FromBody] AiPromptDto body)
    {
        var prompt = await _db.AiPrompts.FindAsync(id);
        if (prompt is null) return NotFound(new { error = "Prompt not found" });
        prompt.SystemPrompt = body.SystemPrompt;
        prompt.UserPromptTemplate = body.UserPromptTemplate;
        prompt.IsActive = body.IsActive;
        prompt.DefaultModelId = body.DefaultModelId;
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
    public int? DefaultModelId { get; set; }
}
