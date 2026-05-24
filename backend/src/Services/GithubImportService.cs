using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Services;

public class GithubImportService : BackgroundService
{
    private readonly Channel<GithubImportRequest> _channel = Channel.CreateBounded<GithubImportRequest>(10);
    private readonly ConcurrentDictionary<Guid, ImportStatus> _statuses = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<GithubImportService> _logger;

    public GithubImportService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory, ILogger<GithubImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public Guid Enqueue(Guid userId, string username, string? token)
    {
        var jobId = Guid.NewGuid();
        _statuses[jobId] = new ImportStatus { JobId = jobId, Status = "queued" };

        var request = new GithubImportRequest(jobId, userId, username, token);
        if (!_channel.Writer.TryWrite(request))
        {
            _statuses.TryRemove(jobId, out _);
            throw new InvalidOperationException("Import queue is full. Please try again later.");
        }

        return jobId;
    }

    public ImportStatus? GetStatus(Guid jobId)
    {
        _statuses.TryGetValue(jobId, out var status);
        return status;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GithubImportService background service started");
        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = ProcessImportAsync(request, stoppingToken);
        }
    }

    private async Task ProcessImportAsync(GithubImportRequest request, CancellationToken ct)
    {
        var (jobId, userId, username, token) = request;
        var status = _statuses[jobId];
        status.Status = "running";
        _logger.LogInformation("GitHub import {JobId} started for user {Username}", jobId, username);

        try
        {
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("User-Agent", "42jobs");
            if (!string.IsNullOrWhiteSpace(token))
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var reposUrl = !string.IsNullOrWhiteSpace(token)
                ? $"https://api.github.com/user/repos?per_page=100&sort=updated&type=all"
                : $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=100&sort=updated";
            var reposJson = await http.GetStringAsync(reposUrl, ct);
            using var reposDoc = JsonDocument.Parse(reposJson);
            var repos = reposDoc.RootElement.EnumerateArray().ToList();

            status.Total = repos.Count;
            status.Message = "Fetching repositories...";

            var projectTexts = new List<(string name, string text)>();

            foreach (var repo in repos)
            {
                ct.ThrowIfCancellationRequested();

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

            status.Message = $"Analyzing {projectTexts.Count} projects with AI...";

            List<GithubProjectResult> projects;
            string error;
            using (var aiScope = _scopeFactory.CreateScope())
            {
                var ai = aiScope.ServiceProvider.GetRequiredService<IAiService>();
                var allText = string.Join("\n\n---\n\n", projectTexts.Select((t, i) => $"PROJECT {i}: {t.name}\n{t.text}"));
                (projects, error) = await ai.AnalyzeGithubProjectsAsync(allText, ct);
            }

            if (!string.IsNullOrEmpty(error))
            {
                status.Status = "failed";
                status.Error = error;
                return;
            }

            status.Message = "Saving projects...";
            int inserted = 0;

            using (var saveScope = _scopeFactory.CreateScope())
            {
                var db = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var proj in projects)
                {
                    ct.ThrowIfCancellationRequested();

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
                        await db.SaveChangesAsync(ct);
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

                        var kw = await db.Keywords.FirstOrDefaultAsync(k => k.Name == name, ct);
                        if (kw is null)
                        {
                            kw = new Keyword { Name = name };
                            db.Keywords.Add(kw);
                            await db.SaveChangesAsync(ct);
                        }

                        await db.Database.ExecuteSqlRawAsync(
                            "INSERT INTO project_keywords (project_id, keyword_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING",
                            ct, project.Id, kw.Id);
                    }

                    inserted++;
                }
            }

            status.Status = "completed";
            status.Inserted = inserted;
            status.Message = $"{inserted} projects imported";

            _logger.LogInformation("GitHub import {JobId}: {Inserted} projects from {Username}", jobId, inserted, username);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GitHub import {JobId} cancelled", jobId);
            status.Status = "failed";
            status.Error = "Import was cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GitHub import {JobId} failed", jobId);
            status.Status = "failed";
            status.Error = ex.Message;
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
}

public record GithubImportRequest(Guid JobId, Guid UserId, string Username, string? Token);

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
