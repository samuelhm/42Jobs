using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("fetch-all-categories")]
    public async Task<IActionResult> FetchAllCategories()
    {
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
