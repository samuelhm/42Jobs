using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public partial class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly AppDbContext _db;
    private readonly GithubImportService _githubImport;

    public ProjectsController(ILogger<ProjectsController> logger, AppDbContext db, GithubImportService githubImport)
    {
        _logger = logger;
        _db = db;
        _githubImport = githubImport;
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

public class ImportGithubDto
{
    public string Username { get; set; } = "";
    public string? Token { get; set; }
}

public class ImportStatus
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = "queued";
    public int Processed { get; set; }
    public int Total { get; set; }
    public int Inserted { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
