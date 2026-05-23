using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{categoryId:int}/fetch/{jobId:guid}")]
    public IActionResult GetFetchStatus([FromRoute] int categoryId, [FromRoute] Guid jobId)
    {
        var status = _fetchService.GetStatus(jobId);
        if (status is null)
        {
            return NotFound(new { error = "Fetch job not found" });
        }

        return Ok(status);
    }
}
