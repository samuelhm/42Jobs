using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class CategoriesController
{
    [HttpGet("{id:int}/fetch-status")]
    public IActionResult GetFetchStatus([FromRoute] int id)
    {
        var userId = GetUserId();
        var follows = _db.UserCategories.Any(uc => uc.UserId == userId && uc.CategoryId == id);
        if (!follows)
            return NotFound(new { error = "Category not found" });

        var isFetching = _fetchService.IsCategoryFetching(id);
        return Ok(new { success = true, data = new { is_fetching = isFetching } });
    }
}
