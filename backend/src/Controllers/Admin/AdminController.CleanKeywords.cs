using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace src.Controllers;

public partial class AdminController
{
    private const int CleanChunkSize = 150;

    [HttpPost("clean-keywords")]
    public async Task<IActionResult> CleanKeywords()
    {
        var cleanErrors = await _readiness.CheckAsync("clean_keywords");
        if (cleanErrors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", cleanErrors) });

        var allNames = await _db.Keywords.OrderBy(k => k.Name).Select(k => k.Name).ToListAsync();
        if (allNames.Count == 0)
            return Ok(new { message = "No keywords to clean", removed = 0 });

        var chunks = allNames
            .Select((name, i) => new { name, i })
            .GroupBy(x => x.i / CleanChunkSize)
            .Select(g => g.Select(x => x.name).ToList())
            .ToList();

        var toRemove = new List<string>();
        for (var i = 0; i < chunks.Count; i++)
        {
            try
            {
                var remove = await _ai.CleanKeywordsAsync(chunks[i]);
                toRemove.AddRange(remove);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clean keywords chunk {Chunk}/{Total} failed", i + 1, chunks.Count);
            }

            if (i < chunks.Count - 1)
                await Task.Delay(2000);
        }

        if (toRemove.Count == 0)
            return Ok(new { message = "No keywords to remove", removed = 0 });

        var distinctNames = toRemove.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Safety net: never auto-remove compound keywords (2+ words).
        // Single-word flags ("coding", "experience") are reliable;
        // multi-word flags ("code review", "azure services") are often
        // incorrect with smaller AI models.
        var singleWord = distinctNames.Where(n => !n.Contains(' ')).ToList();
        var multiWord = distinctNames.Where(n => n.Contains(' ')).ToList();
        if (multiWord.Count > 0)
        {
            _logger.LogWarning(
                "Clean keywords: AI suggested removing {Count} compound keywords. Skipped per safety net. Run dedup manually: {Names}",
                multiWord.Count, string.Join(", ", multiWord.Take(20)));
        }

        // Upsert into blocked_keywords so they never come back
        foreach (var batch in singleWord.Chunk(100))
        {
            var valuesBlocked = new List<string>();
            var parametersBlocked = new List<Npgsql.NpgsqlParameter>();
            for (int i = 0; i < batch.Length; i++)
            {
                parametersBlocked.Add(new Npgsql.NpgsqlParameter($"@b{i}", batch[i].ToLowerInvariant()));
                valuesBlocked.Add($"(@b{i})");
            }
            var sqlBlocked = $@"
                INSERT INTO blocked_keywords (name)
                VALUES {string.Join(", ", valuesBlocked)}
                ON CONFLICT (name) DO NOTHING";
            await _db.Database.ExecuteSqlRawAsync(sqlBlocked, parametersBlocked.Cast<object>().ToArray());
        }

        // Delete the keywords themselves (associations cascade)
        foreach (var batch in singleWord.Chunk(100))
        {
            await _db.Keywords.Where(k => batch.Contains(k.Name)).ExecuteDeleteAsync();
        }

        _ = _adminLog.LogAsync(
            actor: "admin",
            action: "clean_keywords",
            payload1: new { removed = singleWord.Count, skipped_multiword = multiWord.Count, names = singleWord },
            payload2: null,
            payload3: null);

        return Ok(new { message = $"Blocked and removed {singleWord.Count} low-quality keywords ({multiWord.Count} multi-word skipped for safety)", removed = singleWord.Count, skipped_multiword = multiWord.Count });
    }
}
