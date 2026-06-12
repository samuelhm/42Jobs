using Microsoft.EntityFrameworkCore;

namespace src.Data;

public partial class AppDbContext
{
    public async Task<HashSet<int>> GetSignificantKeywordIdsAsync(double threshold = 0.02)
    {
        var categories = await Categories
            .Where(c => c.Jobs.Any())
            .Select(c => new { c.Id, Total = c.Jobs.Count })
            .ToListAsync();

        if (categories.Count == 0) return [];

        var significantIds = new HashSet<int>();

        foreach (var cat in categories)
        {
            var minJobs = Math.Max(1, (int)Math.Ceiling(cat.Total * threshold));
            var ids = await Keywords
                .Where(k => k.Jobs.Count(j => j.Categories.Any(c => c.Id == cat.Id)) >= minJobs)
                .Select(k => k.Id)
                .ToListAsync();
            significantIds.UnionWith(ids);
        }

        return significantIds;
    }
}
