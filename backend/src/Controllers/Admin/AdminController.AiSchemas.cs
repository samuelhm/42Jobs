using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("ai-schemas")]
    public async Task<IActionResult> GetAiSchemas()
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var schemas = await _db.AiSchemas.OrderBy(s => s.Name).ToListAsync();
        return Ok(new { success = true, data = schemas.Select(s => new
        {
            s.Id, s.Name, s.Description,
            JsonSchema = JsonDocument.Parse(s.JsonSchema).RootElement,
            s.CreatedAt, s.UpdatedAt
        }) });
    }

    [HttpPut("ai-schemas/{id:int}")]
    public async Task<IActionResult> UpdateAiSchema([FromRoute] int id, [FromBody] AiSchemaDto body)
    {
        var check = EnsureAdmin(); if (check is not null) return check;
        var schema = await _db.AiSchemas.FindAsync(id);
        if (schema is null) return NotFound(new { error = "Schema not found" });
        schema.JsonSchema = body.JsonSchema;
        schema.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = schema });
    }
}

public class AiSchemaDto
{
    public string JsonSchema { get; set; } = "{}";
}
