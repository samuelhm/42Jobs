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
        var models = await _db.AiModels.Include(m => m.AiService).OrderBy(m => m.AiService.Name).ThenBy(m => m.Name).ToListAsync();
        return Ok(new { success = true, data = models.Select(m => new
        {
            m.Id, m.Name, m.IsActive, m.IsDefault,
            AiServiceName = m.AiService.Name,
            m.AiServiceId
        }) });
    }

    [HttpPost("ai-models")]
    public async Task<IActionResult> CreateAiModel([FromBody] AiModelDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var model = new AiModel { AiServiceId = body.AiServiceId, Name = body.Name, IsActive = body.IsActive, IsDefault = body.IsDefault };
        if (body.IsDefault)
        {
            await _db.AiModels.ExecuteUpdateAsync(m => m.SetProperty(x => x.IsDefault, false));
        }
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
        if (body.IsDefault && !model.IsDefault)
        {
            await _db.AiModels.ExecuteUpdateAsync(m => m.SetProperty(x => x.IsDefault, false));
        }
        model.IsDefault = body.IsDefault;
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
    public bool IsDefault { get; set; }
}
