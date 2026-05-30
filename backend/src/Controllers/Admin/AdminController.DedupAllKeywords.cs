using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("dedup-keywords-all")]
    public async Task<IActionResult> DedupKeywordsAll()
    {
        var dedupErrors = await _readiness.CheckAsync("dedup_keywords");
        if (dedupErrors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", dedupErrors) });

        var allKeywords = await _db.Keywords.OrderBy(k => k.Name).ToListAsync();
        if (allKeywords.Count < 2)
            return Ok(new { message = "Not enough keywords to deduplicate", merged = 0 });

        var merged = 0;
        var warnings = new List<string>();
        var mergedEntries = new List<(string kept, string duplicate)>();

        var heuristicGroups = allKeywords
            .Select(k => (keyword: k, normalized: NormalizeKeyword(k.Name)))
            .GroupBy(x => x.normalized)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in heuristicGroups)
        {
            var sorted = group
                .OrderBy(x => x.keyword.Name.Length)
                .ThenBy(x => x.keyword.Id)
                .ToList();
            var keep = sorted[0].keyword;

            foreach (var dup in sorted.Skip(1))
            {
                if (dup.keyword.Id == keep.Id) continue;
                await MergeKeywordAsync(keep.Id, dup.keyword.Id);
                mergedEntries.Add((keep.Name, dup.keyword.Name));
                merged++;
            }
        }

        if (merged > 0)
            _logger.LogInformation("Heuristic pre-filter merged {Count} keywords in {Groups} groups", merged, heuristicGroups.Count);

        var remaining = await _db.Keywords.OrderBy(k => k.Name).ToListAsync();
        if (remaining.Count < 2)
            return BuildResult(merged, warnings, mergedEntries, "dedup_keywords_all");

        var names = remaining.Select(k => k.Name).ToList();

        List<List<string>> allGroups;
        try
        {
            allGroups = await _ai.DedupKeywordsAsync(names);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Full dedup AI call failed, keeping keywords as-is");
            allGroups = names.Select(k => new List<string> { k }).ToList();
        }

        var dbNames = new HashSet<string>(remaining.Select(k => k.Name), StringComparer.OrdinalIgnoreCase);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in allGroups)
        {
            foreach (var name in group)
            {
                if (!dbNames.Contains(name))
                    warnings.Add($"AI returned unknown keyword '{name}', skipping");
                if (!seenNames.Add(name))
                    warnings.Add($"Keyword '{name}' appears in multiple AI groups, may cause data loss");
            }
        }

        var alreadyMerged = new HashSet<int>();

        foreach (var group in allGroups)
        {
            if (group.Count < 2) continue;

            var keep = remaining.FirstOrDefault(k => k.Name.Equals(group[0], StringComparison.OrdinalIgnoreCase));
            if (keep is null)
            {
                warnings.Add($"Canonical keyword '{group[0]}' not found in DB, skipping group");
                continue;
            }

            if (alreadyMerged.Contains(keep.Id))
            {
                warnings.Add($"Canonical keyword '{keep.Name}' was already merged as duplicate in another group, skipping");
                continue;
            }

            alreadyMerged.Add(keep.Id);

            foreach (var dupName in group.Skip(1))
            {
                var dup = remaining.FirstOrDefault(k => k.Name.Equals(dupName, StringComparison.OrdinalIgnoreCase));
                if (dup is null || dup.Id == keep.Id) continue;
                if (alreadyMerged.Contains(dup.Id))
                {
                    warnings.Add($"Keyword '{dup.Name}' was canonical in another group, skipping to avoid double-merge");
                    continue;
                }

                await MergeKeywordAsync(keep.Id, dup.Id);
                mergedEntries.Add((keep.Name, dupName));
                merged++;
                alreadyMerged.Add(dup.Id);

                await SaveDedupMapping(dupName, keep.Id);
            }
        }

        return BuildResult(merged, warnings, mergedEntries, "dedup_keywords_all");
    }
}
