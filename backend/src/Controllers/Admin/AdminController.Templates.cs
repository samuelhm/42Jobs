using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates()
    {
        var templates = await _db.CvTemplates.OrderByDescending(t => t.UpdatedAt).ToListAsync();
        return Ok(new { success = true, data = templates });
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] CvTemplateDto body)
    {
        var template = new CvTemplate
        {
            Name = body.Name, Description = body.Description,
            HtmlTemplate = body.HtmlTemplate, Css = body.Css, IsActive = body.IsActive
        };
        if (body.IsActive)
        {
            await _db.CvTemplates.ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));
        }
        _db.CvTemplates.Add(template);
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = template });
    }

    [HttpPut("templates/{id:int}")]
    public async Task<IActionResult> UpdateTemplate([FromRoute] int id, [FromBody] CvTemplateDto body)
    {
        var template = await _db.CvTemplates.FindAsync(id);
        if (template is null) return NotFound(new { error = "Template not found" });
        template.Name = body.Name;
        template.Description = body.Description;
        template.HtmlTemplate = body.HtmlTemplate;
        template.Css = body.Css;
        if (body.IsActive && !template.IsActive)
        {
            await _db.CvTemplates.ExecuteUpdateAsync(t => t.SetProperty(x => x.IsActive, false));
        }
        template.IsActive = body.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true, data = template });
    }

    [HttpDelete("templates/{id:int}")]
    public async Task<IActionResult> DeleteTemplate([FromRoute] int id)
    {
        var template = await _db.CvTemplates.FindAsync(id);
        if (template is null) return NotFound(new { error = "Template not found" });
        _db.CvTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class CvTemplateDto
{
    [Required(ErrorMessage = "Template name is required")]
    [MaxLength(200, ErrorMessage = "Template name must be at most 200 characters")]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Required(ErrorMessage = "HTML template is required")]
    public string HtmlTemplate { get; set; } = "";

    public string? Css { get; set; }
    public bool IsActive { get; set; }
}
