using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpGet("import-github/{jobId:guid}")]
    public IActionResult GetImportStatus(Guid jobId)
    {
        if (ImportStatuses.TryGetValue(jobId, out var status))
            return Ok(status);

        return NotFound(new { error = "Import job not found" });
    }
}
