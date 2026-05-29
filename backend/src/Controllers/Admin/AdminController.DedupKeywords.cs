using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class AdminController
{
    private const int DedupChunkSize = 150;
    private const int DedupChunkOverlap = 15;

    [HttpPost("dedup-keywords")]
    public async Task<IActionResult> DedupKeywords()
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

        // ═══════════════════════════════════════════════════════════
        // Phase 1: Heuristic pre-filter
        // Catch trivial duplicates (versions, formatting, case)
        // without spending AI tokens.
        // ═══════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════
        // Phase 2: AI-based dedup on remaining keywords
        // Reload from DB so we don't work with stale EF-tracked entities
        // that were already removed in Phase 1.
        // ═══════════════════════════════════════════════════════════

        var remaining = await _db.Keywords.OrderBy(k => k.Name).ToListAsync();
        if (remaining.Count < 2)
            return BuildResult(merged, warnings, mergedEntries);

        var names = remaining.Select(k => k.Name).ToList();
        var chunks = BuildSmartChunks(names, DedupChunkSize, DedupChunkOverlap);

        var allGroups = new List<List<string>>();
        for (var i = 0; i < chunks.Count; i++)
        {
            try
            {
                var groups = await _ai.DedupKeywordsAsync(chunks[i]);
                allGroups.AddRange(groups);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dedup chunk {Chunk}/{Total} failed, keeping keywords as-is", i + 1, chunks.Count);
                allGroups.AddRange(chunks[i].Select(k => new List<string> { k }));
            }

            if (i < chunks.Count - 1)
                await Task.Delay(2000);
        }

        // ═══════════════════════════════════════════════════════════
        // Phase 3: Validate AI output
        // ═══════════════════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════════════════
        // Phase 4: Apply AI merges
        // ═══════════════════════════════════════════════════════════

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

                // Save mapping so future dedups skip AI
                await SaveDedupMapping(dupName, keep.Id);
            }
        }

        return BuildResult(merged, warnings, mergedEntries);
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Merges a duplicate keyword into a canonical one.
    /// All M2M associations are migrated, then the duplicate is deleted.
    /// Runs inside a transaction for atomicity.
    /// </summary>
    private async Task MergeKeywordAsync(int keepId, int dupId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var sql = new StringBuilder();

            sql.AppendFormat("INSERT INTO job_keywords (job_id, keyword_id) SELECT job_id, {0} FROM job_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING;", keepId, dupId);
            sql.AppendFormat("DELETE FROM job_keywords WHERE keyword_id = {0};", dupId);

            sql.AppendFormat("INSERT INTO project_keywords (project_id, keyword_id) SELECT project_id, {0} FROM project_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING;", keepId, dupId);
            sql.AppendFormat("DELETE FROM project_keywords WHERE keyword_id = {0};", dupId);

            sql.AppendFormat("INSERT INTO work_experience_keywords (experience_id, keyword_id) SELECT experience_id, {0} FROM work_experience_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING;", keepId, dupId);
            sql.AppendFormat("DELETE FROM work_experience_keywords WHERE keyword_id = {0};", dupId);

            sql.AppendFormat("INSERT INTO user_keywords (user_id, keyword_id, learning_status) SELECT user_id, {0}, learning_status FROM user_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING;", keepId, dupId);
            sql.AppendFormat("DELETE FROM user_keywords WHERE keyword_id = {0};", dupId);

            sql.AppendFormat("DELETE FROM keywords WHERE id = {0};", dupId);

            await _db.Database.ExecuteSqlRawAsync(sql.ToString());
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Normalizes a keyword name for heuristic comparison.
    /// "React 18" → "react", "node.js" → "node js"
    /// Note: does NOT strip + or # — those are part of real
    /// language names (C++, C#, F#) and must not be merged.
    /// </summary>
    private static string NormalizeKeyword(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var n = name.ToLowerInvariant().Trim();

        // Strip version numbers: "react 18", "python 3.11", ".net 6"
        n = VersionRegex().Replace(n, "");

        // Replace formatting chars with spaces: "react.js" → "react js"
        // Deliberately excludes + and # to protect C++, C#, F#, etc.
        n = SpecialCharsRegex().Replace(n, " ");

        // Collapse multiple spaces
        n = MultiSpaceRegex().Replace(n, " ").Trim();

        return n;
    }

    /// <summary>
    /// Builds chunks with overlap so keywords near chunk boundaries
    /// are seen by the AI in both adjacent chunks.
    /// </summary>
    private static List<List<string>> BuildSmartChunks(List<string> sorted, int size, int overlap)
    {
        var chunks = new List<List<string>>();
        var start = 0;
        while (start < sorted.Count)
        {
            var chunk = sorted.Skip(start).Take(size).ToList();
            chunks.Add(chunk);
            start += size - overlap;
            if (start >= sorted.Count) break;
            if (size <= overlap) start += size; // safety: prevent infinite loop
        }
        return chunks;
    }

    private async Task SaveDedupMapping(string dupName, int keepId)
    {
        try
        {
            var name = dupName.ToLowerInvariant();
            var exists = await _db.BlockedKeywords.AnyAsync(b => b.Name == name);
            if (!exists)
            {
                _db.BlockedKeywords.Add(new Models.BlockedKeyword
                {
                    Name = name,
                    RedirectTo = keepId
                });
                await _db.SaveChangesAsync();
            }
        }
        catch
        {
            // ignore duplicates (race condition)
        }
    }

    private IActionResult BuildResult(int merged, List<string> warnings, List<(string kept, string duplicate)> entries)
    {
        var message = merged > 0
            ? $"Merged {merged} duplicate keywords"
            : "No duplicates found";

        // Fire-and-forget admin log (must not block the response)
        _ = _adminLog.LogAsync(
            actor: "system",
            action: "dedup_keywords",
            payload1: new { merged, groups = entries.Select(e => new { kept = e.kept, duplicate = e.duplicate }).ToList() },
            payload2: warnings.Count > 0 ? string.Join("; ", warnings) : null,
            payload3: null);

        return Ok(new { message, merged, warnings = warnings.Count > 0 ? warnings : null });
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+\d+(\.\d+)*$")]
    private static partial Regex VersionRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"[\.\-\/]")]
    private static partial Regex SpecialCharsRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();
}
