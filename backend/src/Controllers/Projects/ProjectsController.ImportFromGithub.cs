using Microsoft.AspNetCore.Mvc;
using src.Services;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpPost("import-github")]
    public async Task<IActionResult> ImportFromGithub([FromBody] ImportGithubDto body)
    {
        var userId = GetUserId();
        var username = body.Username.Trim();
        if (string.IsNullOrEmpty(username))
            return BadRequest(new { error = "Username is required" });

        var ghErrors = await _readiness.CheckAsync("analyze_github");
        if (ghErrors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", ghErrors) });

        var jobId = _githubImport.Enqueue(userId, username, body.Token);

        return Accepted(new { job_id = jobId, status = "queued", status_url = $"/api/projects/import-github/{jobId}" });
    }
}
