using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace src.Controllers;

public partial class AdminController
{
    private const int ReExtractChunkSize = 20;

    [HttpPost("re-extract-keywords")]
    public async Task<IActionResult> ReExtractKeywords()
    {
        var errors = await _readiness.CheckAsync("extract_keywords");
        if (errors.Count > 0)
            return StatusCode(503, new { error = string.Join("; ", errors) });

        var discardedIds = await _db.DiscardedJobs
            .Select(d => new { d.ExternalId, d.Source })
            .ToListAsync();

        var discardedKeys = new HashSet<string>(
            discardedIds.Select(d => $"{d.Source}|{d.ExternalId}"));

        var jobs = await _db.Jobs
            .OrderBy(j => j.Id)
            .Select(j => new { j.Id, j.Title, j.Benefits, j.Description, j.ExternalId, j.Source })
            .ToListAsync();

        var toProcess = jobs
            .Where(j => !discardedKeys.Contains($"{j.Source}|{j.ExternalId}"))
            .ToList();

        if (toProcess.Count == 0)
            return Ok(new { message = "No jobs to process", processed = 0, keywords = 0 });

        var blocked = await _db.BlockedKeywords.ToDictionaryAsync(b => b.Name, b => b.RedirectTo);
        var totalKeywords = 0;
        var processed = 0;

        for (var i = 0; i < toProcess.Count; i += ReExtractChunkSize)
        {
            var chunk = toProcess.Skip(i).Take(ReExtractChunkSize).ToList();

            foreach (var job in chunk)
            {
                try
                {
                    var parts = new List<string?> { job.Title, job.Benefits, job.Description };
                    var inputText = string.Join(". ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

                    if (string.IsNullOrWhiteSpace(inputText)) continue;

                    var (skills, _) = await _ai.ExtractKeywordsAsync(inputText);

                    var names = skills
                        .Select(n => n.Trim().ToLowerInvariant())
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Distinct()
                        .ToList();

                    if (names.Count == 0) continue;

                    var finalNames = new List<string>();
                    foreach (var name in names)
                    {
                        if (blocked.TryGetValue(name, out var redirect))
                        {
                            if (redirect is null) continue;
                            var target = await _db.Keywords.FindAsync(redirect.Value);
                            if (target is not null)
                                finalNames.Add(target.Name);
                            continue;
                        }
                        finalNames.Add(name);
                    }
                    finalNames = finalNames.Distinct().ToList();

                    if (finalNames.Count == 0) continue;

                    var keywordIds = await BatchUpsertKeywordsAsync(finalNames);
                    await BatchLinkJobKeywordsAsync(job.Id, keywordIds);
                    totalKeywords += finalNames.Count;
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Re-extract keywords failed for job {JobId} \"{Title}\"",
                        job.Id, job.Title);
                }
            }

            if (i + ReExtractChunkSize < toProcess.Count)
                await Task.Delay(2000);
        }

        _ = _adminLog.LogAsync(
            actor: "admin",
            action: "re_extract_keywords",
            payload1: new { processed, totalKeywords },
            payload2: null,
            payload3: null);

        return Ok(new
        {
            message = $"Re-extracted keywords for {processed} jobs ({totalKeywords} keywords total)",
            processed,
            keywords = totalKeywords
        });
    }

    private async Task<List<int>> BatchUpsertKeywordsAsync(List<string> names)
    {
        var valuesList = new List<string>();
        var parameters = new List<NpgsqlParameter>();
        for (int i = 0; i < names.Count; i++)
        {
            parameters.Add(new NpgsqlParameter($"@p{i}", names[i]));
            valuesList.Add($"(@p{i})");
        }

        var sql = $@"
            INSERT INTO keywords (name)
            VALUES {string.Join(", ", valuesList)}
            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name
            RETURNING id";

        var conn = _db.Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in parameters) cmd.Parameters.Add(p);

            var ids = new List<int>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) ids.Add(reader.GetInt32(0));
            return ids;
        }
        finally
        {
            if (wasClosed && conn.State == ConnectionState.Open)
                await conn.CloseAsync();
        }
    }

    private async Task BatchLinkJobKeywordsAsync(int jobId, List<int> keywordIds)
    {
        if (keywordIds.Count == 0) return;

        var valuesList = new List<string>();
        for (int i = 0; i < keywordIds.Count; i++)
            valuesList.Add($"({jobId}, {keywordIds[i]})");

        var sql = $@"
            INSERT INTO job_keywords (job_id, keyword_id)
            VALUES {string.Join(", ", valuesList)}
            ON CONFLICT DO NOTHING";

        await _db.Database.ExecuteSqlRawAsync(sql);
    }
}
