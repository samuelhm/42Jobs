using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class KeywordsController
{
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] UpdateKeywordDto body)
    {
        var userId = GetUserId();
        var keyword = await _db.Keywords.FindAsync(id);
        if (keyword is null) return NotFound(new { success = false, error = "Keyword not found" });

        if (body.LearningStatus is not null)
        {
            var userKw = await _db.UserKeywords
                .FirstOrDefaultAsync(uk => uk.UserId == userId && uk.KeywordId == id);

            if (userKw is null)
            {
                userKw = new UserKeyword
                {
                    UserId = userId,
                    KeywordId = id,
                    LearningStatus = body.LearningStatus
                };
                _db.UserKeywords.Add(userKw);
            }
            else
            {
                userKw.LearningStatus = body.LearningStatus;
            }

            await _db.SaveChangesAsync();
        }

        return Ok(new
        {
            success = true,
            data = new KeywordResponseDto
            {
                Id = keyword.Id,
                Name = keyword.Name,
                LearningStatus = body.LearningStatus ?? "not_learned"
            }
        });
    }
}
