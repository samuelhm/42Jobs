using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly AppDbContext _db;

    public ProjectsController(ILogger<ProjectsController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var projects = await _db.Projects
            .Where(p => p.UserId == userId)
            .Include(p => p.Keywords)
            .Select(p => new ProjectResponseDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Type = p.Type,
                Keywords = p.Keywords.Select(k => k.Name).ToList()
            })
            .ToListAsync();

        return Ok(new { success = true, data = projects });
    }

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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        if (project is null) return NotFound(new { success = false, error = "Project not found" });

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private async Task SyncProjectKeywords(int projectId, List<int> keywordIds)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM project_keywords WHERE project_id = {0}", projectId);

        foreach (var kwId in keywordIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO project_keywords (project_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                projectId, kwId);
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
