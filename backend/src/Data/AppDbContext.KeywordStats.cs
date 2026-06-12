using Microsoft.EntityFrameworkCore;

namespace src.Data;

public partial class AppDbContext
{
    public async Task<HashSet<int>> GetSignificantKeywordIdsAsync(double threshold = 0.05)
    {
        var totalJobs = await Jobs.CountAsync();
        if (totalJobs == 0) return [];

        var minJobs = Math.Max(1, (int)Math.Ceiling(totalJobs * threshold));

        var ids = await Keywords
            .Where(k => k.Jobs.Count >= minJobs)
            .Select(k => k.Id)
            .ToListAsync();

        return [..ids];
    }
}
