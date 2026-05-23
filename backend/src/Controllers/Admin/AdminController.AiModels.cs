using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-models")]
    public async Task<IActionResult> GetAiModels()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var models = await _db.AiModels.Include(m => m.AiService).AsNoTracking().OrderBy(m => m.AiService.Name).ThenBy(m => m.Name).ToListAsync();
        var prompts = await _db.AiPrompts.Include(p => p.DefaultModel).AsNoTracking().ToListAsync();
        return Ok(new { success = true, data = models.Select(m => new
        {
            m.Id, m.Name, m.IsActive,
            ai_service_name = m.AiService.Name,
            m.AiServiceId,
            used_by = prompts.Where(p => p.DefaultModelId == m.Id).Select(p => p.Functionality).ToList()
        }).ToList() });
    }

    [HttpPost("ai-models")]
    public async Task<IActionResult> CreateAiModel([FromBody] AiModelDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var model = new AiModel { AiServiceId = body.AiServiceId, Name = body.Name, IsActive = body.IsActive };
        _db.AiModels.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = model });
    }

    [HttpPut("ai-models/{id:int}")]
    public async Task<IActionResult> UpdateAiModel([FromRoute] int id, [FromBody] AiModelDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var model = await _db.AiModels.FindAsync(id);
        if (model is null) return NotFound(new { error = "Model not found" });
        model.Name = body.Name;
        model.AiServiceId = body.AiServiceId;
        model.IsActive = body.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = model });
    }

    [HttpDelete("ai-models/{id:int}")]
    public async Task<IActionResult> DeleteAiModel([FromRoute] int id)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var model = await _db.AiModels.FindAsync(id);
        if (model is null) return NotFound(new { error = "Model not found" });
        _db.AiModels.Remove(model);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class AiModelDto
{
    public int AiServiceId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
