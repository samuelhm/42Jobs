using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("clean-keywords-all")]
    public async Task<IActionResult> CleanKeywordsAll()
    {
        var cleanErrors = await _readiness.CheckAsync("clean_keywords");
        if (cleanErrors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", cleanErrors) });

        var allNames = await _db.Keywords.OrderBy(k => k.Name).Select(k => k.Name).ToListAsync();
        if (allNames.Count == 0)
            return Ok(new { message = "No keywords to clean", removed = 0 });

        List<string> toRemove;
        try
        {
            toRemove = await _ai.CleanKeywordsAsync(allNames);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Full clean AI call failed");
            toRemove = [];
        }

        if (toRemove.Count == 0)
            return Ok(new { message = "No keywords to remove", removed = 0 });

        var distinctNames = toRemove.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var singleWord = distinctNames.Where(n => !n.Contains(' ')).ToList();
        var multiWord = distinctNames.Where(n => n.Contains(' ')).ToList();
        if (multiWord.Count > 0)
        {
            _logger.LogWarning(
                "Clean keywords all: AI suggested removing {Count} compound keywords. Skipped per safety net: {Names}",
                multiWord.Count, string.Join(", ", multiWord.Take(20)));
        }

        foreach (var batch in singleWord.Chunk(100))
        {
            var valuesBlocked = new List<string>();
            var parametersBlocked = new List<NpgsqlParameter>();
            for (int i = 0; i < batch.Length; i++)
            {
                parametersBlocked.Add(new NpgsqlParameter($"@b{i}", batch[i].ToLowerInvariant()));
                valuesBlocked.Add($"(@b{i})");
            }
            var sqlBlocked = $@"
                INSERT INTO blocked_keywords (name)
                VALUES {string.Join(", ", valuesBlocked)}
                ON CONFLICT (name) DO NOTHING";
            await _db.Database.ExecuteSqlRawAsync(sqlBlocked, parametersBlocked.Cast<object>().ToArray());
        }

        foreach (var batch in singleWord.Chunk(100))
        {
            await _db.Keywords.Where(k => batch.Contains(k.Name)).ExecuteDeleteAsync();
        }

        _ = _adminLog.LogAsync(
            actor: "admin",
            action: "clean_keywords_all",
            payload1: new { removed = singleWord.Count, skipped_multiword = multiWord.Count, names = singleWord },
            payload2: null,
            payload3: null);

        return Ok(new
        {
            message = $"Blocked and removed {singleWord.Count} low-quality keywords ({multiWord.Count} multi-word skipped for safety)",
            removed = singleWord.Count,
            skipped_multiword = multiWord.Count
        });
    }
}
