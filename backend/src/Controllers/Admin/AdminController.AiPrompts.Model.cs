using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPatch("ai-prompts/{id:int}/model")]
    public async Task<IActionResult> UpdateAiPromptModel([FromRoute] int id, [FromBody] AiPromptModelDto body)
    {
        var prompt = await _db.AiPrompts.FindAsync(id);
        if (prompt is null) return NotFound(new { error = "Prompt not found" });

        if (body.DefaultModelId.HasValue)
        {
            var modelExists = await _db.AiModels.AnyAsync(m => m.Id == body.DefaultModelId.Value && m.IsActive);
            if (!modelExists)
                return BadRequest(new { error = "Model not found or not active" });
        }

        prompt.DefaultModelId = body.DefaultModelId;
        prompt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, data = new { prompt.Id, prompt.Functionality, prompt.DefaultModelId } });
    }
}

public class AiPromptModelDto
{
    public int? DefaultModelId { get; set; }
}
