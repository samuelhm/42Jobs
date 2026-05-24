using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpGet("import-github/{jobId:guid}")]
    public IActionResult GetImportStatus(Guid jobId)
    {
        var status = _githubImport.GetStatus(jobId);
        if (status is not null)
            return Ok(status);

        return NotFound(new { error = "Import job not found" });
    }
}
