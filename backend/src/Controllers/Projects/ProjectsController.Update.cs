using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] ProjectDto body)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (project is null) return NotFound(new { success = false, error = "Project not found" });

        project.Name = body.Name;
        project.Description = body.Description;
        project.Type = body.Type;
        await _db.SaveChangesAsync();

        if (body.KeywordIds is not null)
        {
            await SyncProjectKeywords(project.Id, body.KeywordIds);
        }

        var keywords = body.KeywordIds is not null
            ? await _db.Keywords.Where(k => body.KeywordIds.Contains(k.Id)).Select(k => k.Name).ToListAsync()
            : await _db.Entry(project).Collection(p => p.Keywords).Query().Select(k => k.Name).ToListAsync();

        return Ok(new
        {
            success = true,
            data = new ProjectResponseDto
            {
                Id = project.Id, Name = project.Name, Description = project.Description,
                Type = project.Type, Keywords = keywords
            }
        });
    }
}
