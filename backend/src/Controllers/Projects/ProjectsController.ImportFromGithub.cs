using Microsoft.AspNetCore.Mvc;
using src.Services;

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

        var jobId = _githubImport.Enqueue(userId, username, body.Token);

        return Accepted(new { job_id = jobId, status = "queued", status_url = $"/api/projects/import-github/{jobId}" });
    }
}
