using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("fetch-all-categories")]
    public async Task<IActionResult> FetchAllCategories([FromBody] FetchAllRequest? body)
    {
        if (_fetch.IsFetchAllRunning)
            return StatusCode(409, new { error = "A fetch is already running (scheduler or previous request). Try again later." });

        try
        {
            await _fetch.FetchAllCategoriesAsync("past-week", body?.Location);
            var scope = body?.Location is not null ? $" for {body.Location}" : " for all user locations";
            return Ok(new { message = $"Scheduled fetch triggered{scope}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class FetchAllRequest
{
    public string? Location { get; set; }
}
