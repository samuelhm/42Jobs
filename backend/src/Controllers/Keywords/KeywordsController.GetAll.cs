using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class KeywordsController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();

        var keywords = await _db.Keywords
            .OrderBy(k => k.Name)
            .Select(k => new KeywordResponseDto
            {
                Id = k.Id,
                Name = k.Name,
                LearningStatus = k.UserKeywords
                    .Where(uk => uk.UserId == userId)
                    .Select(uk => (string?)uk.LearningStatus)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { success = true, data = keywords });
    }
}
