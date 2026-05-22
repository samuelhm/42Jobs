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
    private readonly OpenAIService _openAi;

    public AdminController(AppDbContext db, OpenAIService openAi)
    {
        _db = db;
        _openAi = openAi;
    }

    [HttpPost("dedup-keywords")]
    public async Task<IActionResult> DedupKeywords()
    {
        var allKeywords = await _db.Keywords.OrderBy(k => k.Name).ToListAsync();
        if (allKeywords.Count < 2)
            return Ok(new { message = "Not enough keywords to deduplicate", merged = 0 });

        var names = allKeywords.Select(k => k.Name).ToList();
        var result = await _openAi.DedupKeywordsAsync(names);

        int merged = 0;
        foreach (var group in result)
        {
            if (group.Count < 2) continue;

            var keep = allKeywords.First(k => k.Name.Equals(group[0], StringComparison.OrdinalIgnoreCase));
            foreach (var dupName in group.Skip(1))
            {
                var dup = allKeywords.FirstOrDefault(k => k.Name.Equals(dupName, StringComparison.OrdinalIgnoreCase));
                if (dup is null) continue;

                // Migrate all M2M references: add canonical keyword where dup exists,
                // then remove the duplicate. ON CONFLICT handles jobs that had both.
                await _db.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO job_keywords (job_id, keyword_id)
                      SELECT job_id, {0} FROM job_keywords WHERE keyword_id = {1}
                      ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM job_keywords WHERE keyword_id = {0}", dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO project_keywords (project_id, keyword_id)
                      SELECT project_id, {0} FROM project_keywords WHERE keyword_id = {1}
                      ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM project_keywords WHERE keyword_id = {0}", dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO work_experience_keywords (experience_id, keyword_id)
                      SELECT experience_id, {0} FROM work_experience_keywords WHERE keyword_id = {1}
                      ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM work_experience_keywords WHERE keyword_id = {0}", dup.Id);

                await _db.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO user_keywords (user_id, keyword_id, learning_status)
                      SELECT user_id, {0}, learning_status FROM user_keywords WHERE keyword_id = {1}
                      ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM user_keywords WHERE keyword_id = {0}", dup.Id);

                _db.Keywords.Remove(dup);
                merged++;
            }
        }

        if (merged > 0) await _db.SaveChangesAsync();

        return Ok(new { message = $"Merged {merged} duplicate keywords", merged });
    }
}
