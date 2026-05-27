using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpPost("{jobId:int}/regenerate")]
    public async Task<IActionResult> Regenerate([FromRoute] int jobId, [FromBody] RegenerateRequest? body)
    {
        var userId = GetUserId();

        var existing = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == jobId);
        if (existing is null)
            return NotFound(new { error = "No CV generated for this job" });

        if (string.IsNullOrWhiteSpace(existing.JsonData))
            return BadRequest(new { error = "CV has no AI data to regenerate from" });

        var user = await _db.Users
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        var job = await _db.Jobs
            .Include(j => j.Keywords)
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return NotFound();

        var educations = await _db.Educations
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.StartYear)
            .ToListAsync();

        CvTemplate? template;

        if (body?.TemplateId is not null)
        {
            template = await _db.CvTemplates.FindAsync(body.TemplateId);
            if (template is null)
                return BadRequest(new { error = $"Template {body.TemplateId} not found" });
        }
        else
        {
            template = await _db.Set<CvTemplate>().FirstOrDefaultAsync(t => t.IsActive);
        }

        var aiData = JsonDocument.Parse(existing.JsonData).RootElement;
        var html = RenderTemplate(template?.HtmlTemplate, user, job, aiData, educations);

        existing.CvData = html;
        existing.TemplateId = template?.Id;
        existing.Model = existing.Model;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, id = existing.Id, html = existing.CvData, model = existing.Model, templateId = existing.TemplateId });
    }
}

public class RegenerateRequest
{
    public int? TemplateId { get; set; }
}
