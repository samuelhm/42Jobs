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
    private readonly AdminLogService _log;
    private readonly ILogger<GithubImportService> _logger;

    public GithubImportService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory, AdminLogService log, ILogger<GithubImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _log = log;
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
        var correlationId = Guid.NewGuid().ToString("N");
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

            await _log.LogAsync("GitHub", "list_repos",
                new { url = reposUrl, username },
                reposUrl, "sent", correlationId);

            var reposJson = await http.GetStringAsync(reposUrl, ct);
            using var reposDoc = JsonDocument.Parse(reposJson);
            var repos = reposDoc.RootElement.EnumerateArray().ToList();

            await _log.LogAsync("GitHub", "list_repos",
                new { repo_count = repos.Count },
                username, $"received:200, {repos.Count} repos", correlationId);

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

            await _log.LogAsync("GitHub", "fetch_files",
                new { repos_scanned = status.Total, repos_with_readme = projectTexts.Count },
                username, $"scanned {projectTexts.Count}/{status.Total} repos with READMEs", correlationId);

            if (projectTexts.Count == 0)
            {
                status.Status = "completed";
                status.Inserted = 0;
                status.Message = "No repos with README found";
                return;
            }

            const int batchSize = 5;
            var totalBatches = (int)Math.Ceiling(projectTexts.Count / (double)batchSize);
            status.Total = projectTexts.Count;
            status.Processed = 0;
            status.Inserted = 0;

            for (var batch = 0; batch < totalBatches; batch++)
            {
                ct.ThrowIfCancellationRequested();

                var batchItems = projectTexts.Skip(batch * batchSize).Take(batchSize).ToList();
                status.Message = $"Analyzing with AI...";

                List<GithubProjectResult> batchProjects;
                string error;
                using (var aiScope = _scopeFactory.CreateScope())
                {
                    var ai = aiScope.ServiceProvider.GetRequiredService<IAiService>();
                    var readiness = aiScope.ServiceProvider.GetRequiredService<IAiReadinessService>();

                    var ghErrors = await readiness.CheckAsync("analyze_github", ct);
                    if (ghErrors.Count > 0)
                    {
                        status.Status = "failed";
                        status.Error = string.Join("; ", ghErrors);
                        return;
                    }

                    var batchText = string.Join("\n\n---\n\n", batchItems.Select((t, i) => $"PROJECT {i}: {t.name}\n{t.text}"));
                    (batchProjects, error) = await ai.AnalyzeGithubProjectsAsync(batchText, ct);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    status.Status = "failed";
                    status.Error = error;
                    return;
                }

                status.Message = $"Saving projects...";

                using (var saveScope = _scopeFactory.CreateScope())
                {
                    var db = saveScope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var blocked = await db.BlockedKeywords
                        .ToDictionaryAsync(b => b.Name, b => b.RedirectTo, ct);

                    foreach (var proj in batchProjects)
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

                            // Check blocked_keywords: skip blocked, redirect known dupes
                            if (blocked.TryGetValue(name, out var redirect))
                            {
                                if (redirect is null) continue; // permanently blocked
                                var target = await db.Keywords.FindAsync(new object[] { redirect.Value }, ct);
                                if (target is not null)
                                    name = target.Name;
                                else
                                    continue;
                            }

                            var kw = await db.Keywords.FirstOrDefaultAsync(k => k.Name == name, ct);
                            if (kw is null)
                            {
                                kw = new Keyword { Name = name };
                                db.Keywords.Add(kw);
                                await db.SaveChangesAsync(ct);
                            }

                            await db.Database.ExecuteSqlAsync(
                                $"INSERT INTO project_keywords (project_id, keyword_id) VALUES ({project.Id}, {kw.Id}) ON CONFLICT DO NOTHING",
                                ct);

                            var learningStatus = proj.Type == "school" ? "learned_in_school" : "learned_personal_project";
                            await db.Database.ExecuteSqlAsync(
                                $"INSERT INTO user_keywords (user_id, keyword_id, learning_status) VALUES ({userId}, {kw.Id}, {learningStatus}) ON CONFLICT (user_id, keyword_id) DO UPDATE SET learning_status = EXCLUDED.learning_status WHERE user_keywords.learning_status != 'learned_in_school'",
                                ct);
                        }

                        status.Inserted++;
                    }
                }

                status.Processed = status.Inserted;
            }

            status.Status = "completed";
            status.Message = $"{status.Inserted} projects imported";

            await _log.LogAsync("GitHub", "import_completed",
                new { username, repo_count = repos.Count, projects_imported = status.Inserted },
                username, $"completed: {status.Inserted} projects", correlationId);

            _logger.LogInformation("GitHub import {JobId}: {Inserted} projects from {Username}", jobId, status.Inserted, username);
        }
        catch (OperationCanceledException)
        {
            await _log.LogAsync("GitHub", "import_cancelled",
                new { username },
                username, $"error: cancelled", correlationId);
            _logger.LogInformation("GitHub import {JobId} cancelled", jobId);
            status.Status = "failed";
            status.Error = "Import was cancelled";
        }
        catch (Exception ex)
        {
            await _log.LogAsync("GitHub", "import_failed",
                new { username, error = ex.Message },
                username, $"error: {ex.GetBaseException().Message}", correlationId);
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
