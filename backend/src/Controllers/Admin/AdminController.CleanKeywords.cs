using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        foreach (var batch in toRemove.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(100))
        {
            await _db.Keywords.Where(k => batch.Contains(k.Name)).ExecuteDeleteAsync();
        }

        return Ok(new { message = $"Removed {toRemove.Count} low-quality keywords", removed = toRemove.Count });
    }
}
