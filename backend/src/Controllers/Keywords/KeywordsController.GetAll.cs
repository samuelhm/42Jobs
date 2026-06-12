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

        var blockedNames = await _db.BlockedKeywords
            .Select(b => b.Name)
            .ToListAsync();

        var blockedSet = new HashSet<string>(blockedNames);

        var significantIds = await _db.GetSignificantKeywordIdsAsync();

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

        var filtered = keywords
            .Where(k => !blockedSet.Contains(k.Name.ToLowerInvariant()))
            .Where(k => significantIds.Contains(k.Id))
            .ToList();

        return Ok(new { success = true, data = filtered });
    }
}
