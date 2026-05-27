using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("fetch-all-categories")]
    public async Task<IActionResult> FetchAllCategories()
    {
        if (_fetch.IsFetchAllRunning)
            return StatusCode(409, new { error = "A fetch is already running (scheduler or previous request). Try again later." });

        try
        {
            await _fetch.FetchAllCategoriesAsync();
            return Ok(new { message = "Scheduled fetch triggered for all categories" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
