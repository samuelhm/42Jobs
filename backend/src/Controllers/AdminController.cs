using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly GeminiService _gemini;

    public AdminController(AppDbContext db, GeminiService gemini)
    {
        _db = db;
        _gemini = gemini;
    }

    [HttpPost("dedup-keywords")]
    public async Task<IActionResult> DedupKeywords()
    {
        var allKeywords = await _db.Keywords.OrderBy(k => k.Name).ToListAsync();
        if (allKeywords.Count < 2)
            return Ok(new { message = "Not enough keywords to deduplicate", merged = 0 });

        var names = allKeywords.Select(k => k.Name).ToList();
        var result = await _gemini.DedupKeywordsAsync(names);

        int merged = 0;
        foreach (var group in result)
        {
            if (group.Count < 2) continue;

            var keep = allKeywords.First(k => k.Name.Equals(group[0], StringComparison.OrdinalIgnoreCase));
            foreach (var dupName in group.Skip(1))
            {
                var dup = allKeywords.FirstOrDefault(k => k.Name.Equals(dupName, StringComparison.OrdinalIgnoreCase));
                if (dup is null) continue;

                // Migrate all M2M references from dup to keep
                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE job_keywords SET keyword_id = {0} WHERE keyword_id = {1}", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM job_keywords WHERE keyword_id = {1} AND job_id IN (SELECT job_id FROM job_keywords WHERE keyword_id = {0})", keep.Id, dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE project_keywords SET keyword_id = {0} WHERE keyword_id = {1}", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM project_keywords WHERE keyword_id = {1} AND project_id IN (SELECT project_id FROM project_keywords WHERE keyword_id = {0})", keep.Id, dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE work_experience_keywords SET keyword_id = {0} WHERE keyword_id = {1}", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM work_experience_keywords WHERE keyword_id = {1} AND experience_id IN (SELECT experience_id FROM work_experience_keywords WHERE keyword_id = {0})", keep.Id, dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    "UPDATE user_keywords SET keyword_id = {0} WHERE keyword_id = {1}", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM user_keywords WHERE keyword_id = {1} AND user_id IN (SELECT user_id FROM user_keywords WHERE keyword_id = {0})", keep.Id, dup.Id);

                _db.Keywords.Remove(dup);
                merged++;
            }
        }

        if (merged > 0) await _db.SaveChangesAsync();

        return Ok(new { message = $"Merged {merged} duplicate keywords", merged });
    }
}
