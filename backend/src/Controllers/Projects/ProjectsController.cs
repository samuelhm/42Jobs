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
public partial class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly GeminiService _gemini;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProjectsController(ILogger<ProjectsController> logger, AppDbContext db, IHttpClientFactory httpFactory, GeminiService gemini, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _db = db;
        _httpFactory = httpFactory;
        _gemini = gemini;
        _scopeFactory = scopeFactory;
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

    private static async Task<string> FetchReposAsync(HttpClient http, string username)
    {
        var url = $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=30&sort=updated";
        return await http.GetStringAsync(url);
    }

    private static readonly ConcurrentDictionary<Guid, ImportStatus> ImportStatuses = new();

    private static async Task ProcessImportAsync(
        Guid jobId, Guid userId, string username, string token,
        IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory,
        ILogger logger)
    {
        var status = ImportStatuses[jobId];
        status.Status = "running";
        logger.LogInformation("GitHub import {JobId} started for user {Username}", jobId, username);

        try
        {
            using var http = httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("User-Agent", "bimjobsnet");
            if (!string.IsNullOrWhiteSpace(token))
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var reposUrl = !string.IsNullOrWhiteSpace(token)
                ? $"https://api.github.com/user/repos?per_page=100&sort=updated&type=all"
                : $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=100&sort=updated";
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

            List<GithubProjectResult> projects;
            string error;
            using (var geminiScope = scopeFactory.CreateScope())
            {
                var gemini = geminiScope.ServiceProvider.GetRequiredService<GeminiService>();
                var allText = string.Join("\n\n---\n\n", projectTexts.Select((t, i) => $"PROJECT {i}: {t.name}\n{t.text}"));
                (projects, error) = await gemini.AnalyzeGithubProjectsAsync(allText);
            }

            if (!string.IsNullOrEmpty(error))
            {
                status.Status = "failed";
                status.Error = error;
                return;
            }

            status.Message = "Saving projects...";
            int inserted = 0;

            using (var saveScope = scopeFactory.CreateScope())
            {
                var db = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();

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
                    db.Projects.Add(project);

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        db.ChangeTracker.Clear();
                        continue;
                    }

                    foreach (var kwName in proj.Keywords)
                    {
                        var name = kwName.Trim().ToLowerInvariant();
                        if (string.IsNullOrEmpty(name)) continue;

                        var kw = await db.Keywords.FirstOrDefaultAsync(k => k.Name == name);
                        if (kw is null)
                        {
                            kw = new Keyword { Name = name };
                            db.Keywords.Add(kw);
                            await db.SaveChangesAsync();
                        }

                        await db.Database.ExecuteSqlRawAsync(
                            "INSERT INTO project_keywords (project_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                            project.Id, kw.Id);
                    }

                    inserted++;
                }
            }

            status.Status = "completed";
            status.Inserted = inserted;
            status.Message = $"{inserted} projects imported";

            logger.LogInformation("GitHub import {JobId}: {Inserted} projects from {Username}", jobId, inserted, username);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GitHub import {JobId} failed", jobId);
            status.Status = "failed";
            status.Error = ex.Message;
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
