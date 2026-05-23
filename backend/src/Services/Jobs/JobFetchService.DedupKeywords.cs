using Microsoft.EntityFrameworkCore;
using src.Data;

namespace src.Services.Jobs;

public partial class JobFetchService
{
    private static async Task DedupKeywordsAsync(AppDbContext db, IAiService ai, ILogger logger)
    {
        try
        {
            var allKeywords = await db.Keywords.OrderBy(k => k.Name).ToListAsync();
            if (allKeywords.Count < 2) return;

            var names = allKeywords.Select(k => k.Name).ToList();
            var groups = await ai.DedupKeywordsAsync(names);

            int merged = 0;
            foreach (var group in groups)
            {
                if (group.Count < 2) continue;

                var keep = allKeywords.FirstOrDefault(k => k.Name.Equals(group[0], StringComparison.OrdinalIgnoreCase));
                if (keep is null) continue;

                foreach (var dupName in group.Skip(1))
                {
                    var dup = allKeywords.FirstOrDefault(k => k.Name.Equals(dupName, StringComparison.OrdinalIgnoreCase));
                    if (dup is null || dup.Id == keep.Id) continue;

                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO job_keywords (job_id, keyword_id) SELECT job_id, {0} FROM job_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM job_keywords WHERE keyword_id = {0}", dup.Id);

                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO project_keywords (project_id, keyword_id) SELECT project_id, {0} FROM project_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM project_keywords WHERE keyword_id = {0}", dup.Id);

                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO work_experience_keywords (experience_id, keyword_id) SELECT experience_id, {0} FROM work_experience_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM work_experience_keywords WHERE keyword_id = {0}", dup.Id);

                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO user_keywords (user_id, keyword_id, learning_status) SELECT user_id, {0}, learning_status FROM user_keywords WHERE keyword_id = {1} ON CONFLICT DO NOTHING", keep.Id, dup.Id);
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM user_keywords WHERE keyword_id = {0}", dup.Id);

                    db.Keywords.Remove(dup);
                    merged++;
                }
            }

            if (merged > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation("Deduped {Merged} keywords after fetch", merged);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Keyword dedup after fetch failed (non-critical)");
        }
    }
}
