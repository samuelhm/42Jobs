using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectDto body)
    {
        var userId = GetUserId();
        var project = new Project
        {
            UserId = userId,
            Name = body.Name,
            Description = body.Description,
            Type = body.Type
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        if (body.KeywordIds is { Count: > 0 })
        {
            await SyncProjectKeywords(project.Id, body.KeywordIds);
        }

        var keywords = body.KeywordIds is not null
            ? await _db.Keywords.Where(k => body.KeywordIds.Contains(k.Id)).Select(k => k.Name).ToListAsync()
            : [];

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
