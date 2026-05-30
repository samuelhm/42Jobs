using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("block-keyword")]
    [EnableRateLimiting("admin_write")]
    public async Task<IActionResult> BlockKeyword([FromBody] BlockKeywordDto body)
    {
        var keyword = await _db.Keywords.FindAsync(body.KeywordId);
        if (keyword is null)
            return NotFound(new { error = "Keyword not found" });

        int? redirectTo = null;

        if (!string.IsNullOrWhiteSpace(body.RedirectToName))
        {
            var target = await _db.Keywords
                .FirstOrDefaultAsync(k => k.Name.ToLower() == body.RedirectToName.Trim().ToLower());
            if (target is null)
                return BadRequest(new { error = $"Target keyword '{body.RedirectToName}' not found" });
            if (target.Id == keyword.Id)
                return BadRequest(new { error = "Cannot redirect a keyword to itself" });

            redirectTo = target.Id;

            await MergeKeywordAsync(target.Id, keyword.Id);
        }
        else
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM job_keywords WHERE keyword_id = {0}", keyword.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM project_keywords WHERE keyword_id = {0}", keyword.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM work_experience_keywords WHERE keyword_id = {0}", keyword.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM user_keywords WHERE keyword_id = {0}", keyword.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM keywords WHERE id = {0}", keyword.Id);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        _db.BlockedKeywords.Add(new Models.BlockedKeyword
        {
            Name = keyword.Name.ToLowerInvariant(),
            RedirectTo = redirectTo
        });
        await _db.SaveChangesAsync();

        _ = _adminLog.LogAsync(
            actor: "admin",
            action: "block_keyword",
            payload1: new { keyword.Name, redirectTo },
            payload2: null,
            payload3: null);

        return Ok(new { message = $"Keyword '{keyword.Name}' blocked", redirectTo });
    }
}

public class BlockKeywordDto
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid keyword is required")]
    public int KeywordId { get; set; }

    [MaxLength(200, ErrorMessage = "Redirect name must be at most 200 characters")]
    public string? RedirectToName { get; set; }
}
