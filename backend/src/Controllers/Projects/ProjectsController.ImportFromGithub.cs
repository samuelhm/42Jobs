using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpPost("import-github")]
    public IActionResult ImportFromGithub([FromBody] ImportGithubDto body)
    {
        var userId = GetUserId();
        var username = body.Username.Trim();
        if (string.IsNullOrEmpty(username))
            return BadRequest(new { error = "Username is required" });

        var jobId = Guid.NewGuid();
        ImportStatuses[jobId] = new ImportStatus { Status = "queued", JobId = jobId };

        var token = body.Token;

        var scopeFactory = _scopeFactory;
        var httpFactory = _httpFactory;
        var logger = _logger;

        _ = Task.Run(async () => await ProcessImportAsync(jobId, userId, username, token, scopeFactory, httpFactory, logger));

        return Accepted(new { job_id = jobId, status = "queued", status_url = $"/api/projects/import-github/{jobId}" });
    }
}
