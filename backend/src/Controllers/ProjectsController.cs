using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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
public class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly GeminiService _gemini;

    public ProjectsController(ILogger<ProjectsController> logger, AppDbContext db, IHttpClientFactory httpFactory, GeminiService gemini)
    {
        _logger = logger;
        _db = db;
        _httpFactory = httpFactory;
        _gemini = gemini;
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

    private static readonly ConcurrentDictionary<Guid, ImportStatus> ImportStatuses = new();

    [HttpPost("import-github")]
    public IActionResult ImportFromGithub([FromBody] ImportGithubDto body)
    {
        var userId = GetUserId();
        var username = body.Username.Trim();
        if (string.IsNullOrEmpty(username))
            return BadRequest(new { error = "Username is required" });

        var user = _db.Users.Find(userId);
        if (user?.LastGithubImportAt is not null
            && DateTime.UtcNow - user.LastGithubImportAt.Value < TimeSpan.FromHours(24))
        {
            return Ok(new { status = "rate-limited", message = "You can only import once per day" });
        }

        var jobId = Guid.NewGuid();
        ImportStatuses[jobId] = new ImportStatus { Status = "queued", JobId = jobId };

        _ = Task.Run(async () => await ProcessImportAsync(jobId, userId, username));

        return Accepted(new { job_id = jobId, status = "queued", status_url = $"/api/projects/import-github/{jobId}" });
    }

    [HttpGet("import-github/{jobId:guid}")]
    public IActionResult GetImportStatus(Guid jobId)
    {
        if (ImportStatuses.TryGetValue(jobId, out var status))
            return Ok(status);

        return NotFound(new { error = "Import job not found" });
    }

    private async Task ProcessImportAsync(Guid jobId, Guid userId, string username)
    {
        var status = ImportStatuses[jobId];
        status.Status = "running";
        _logger.LogInformation("GitHub import {JobId} started for user {Username}", jobId, username);

        try
        {
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("User-Agent", "bimjobsnet");

            var reposUrl = $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=50&sort=updated";
            var reposJson = await http.GetStringAsync(reposUrl);
            using var reposDoc = JsonDocument.Parse(reposJson);
            var repos = reposDoc.RootElement.EnumerateArray().ToList();

            status.Total = repos.Count;
            status.Message = "Fetching repositories...";

            var projectTexts = new List<(string name, string text)>();

            foreach (var repo in repos)
            {
                var repoName = repo.GetProperty("name").GetString()!;
                var defaultBranch = repo.GetProperty("default_branch").GetString() ?? "main";

                var readme = await TryFetchRaw(http, username, repoName, defaultBranch, "README.md");
                if (string.IsNullOrWhiteSpace(readme))
                {
                    status.Processed++;
                    continue;
                }

                var configs = new List<string>();
                foreach (var file in new[] { "package.json", "requirements.txt", "Makefile", "docker-compose.yml", "go.mod", "Cargo.toml", "pyproject.toml", "CMakeLists.txt" })
                {
                    var content = await TryFetchRaw(http, username, repoName, defaultBranch, file);
                    if (!string.IsNullOrWhiteSpace(content))
                        configs.Add($"{file}:\n{content}");
                }

                var combined = $"# {repoName}\n\nREADME:\n{readme}";
                if (configs.Count > 0)
                    combined += "\n\nConfig files:\n" + string.Join("\n\n", configs);

                projectTexts.Add((repoName, combined));
                status.Processed++;
                status.Message = $"Fetched {status.Processed}/{status.Total} repos...";
            }

            if (projectTexts.Count == 0)
            {
                status.Status = "completed";
                status.Inserted = 0;
                status.Message = "No repos with README found";
                return;
            }

            status.Message = $"Analyzing {projectTexts.Count} projects with Gemini...";

            var allText = string.Join("\n\n---\n\n", projectTexts.Select((t, i) => $"PROJECT {i}: {t.name}\n{t.text}"));
            var (projects, error) = await _gemini.AnalyzeGithubProjectsAsync(allText);

            if (!string.IsNullOrEmpty(error))
            {
                status.Status = "failed";
                status.Error = error;
                return;
            }

            status.Message = "Saving projects...";
            int inserted = 0;

            foreach (var proj in projects)
            {
                if (string.IsNullOrWhiteSpace(proj.Name)) continue;

                var project = new Project
                {
                    UserId = userId,
                    Name = proj.Name,
                    Description = proj.Description,
                    Type = proj.Type == "school" ? "school" : "personal"
                };
                _db.Projects.Add(project);
                await _db.SaveChangesAsync();

                foreach (var kwName in proj.Keywords)
                {
                    var name = kwName.Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(name)) continue;

                    var kw = await _db.Keywords.FirstOrDefaultAsync(k => k.Name == name);
                    if (kw is null)
                    {
                        kw = new Keyword { Name = name };
                        _db.Keywords.Add(kw);
                        await _db.SaveChangesAsync();
                    }

                    await _db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO project_keywords (project_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                        project.Id, kw.Id);
                }

                inserted++;
            }

            var user = await _db.Users.FindAsync(userId);
            if (user is not null)
            {
                user.LastGithubImportAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            status.Status = "completed";
            status.Inserted = inserted;
            status.Message = $"{inserted} projects imported";

            _logger.LogInformation("GitHub import {JobId}: {Inserted} projects from {Username}", jobId, inserted, username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub import {JobId} failed", jobId);
            status.Status = "failed";
            status.Error = ex.Message;
        }
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

    private static async Task<string?> TryFetchRaw(HttpClient http, string owner, string repo, string branch, string path)
    {
        try
        {
            var url = $"https://raw.githubusercontent.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/{Uri.EscapeDataString(branch)}/{path}";
            return await http.GetStringAsync(url);
        }
        catch
        {
            return null;
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
