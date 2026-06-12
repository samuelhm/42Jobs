using Microsoft.EntityFrameworkCore;

namespace src.Data;

public partial class AppDbContext
{
    public async Task<HashSet<int>> GetSignificantKeywordIdsAsync(double threshold = 0.02)
    {
        var ids = await Database
            .SqlQuery<int>($"""
                WITH cat_totals AS (
                    SELECT c.id, COUNT(jc.job_id)::float AS total
                    FROM categories c
                    JOIN job_categories jc ON jc.category_id = c.id
                    GROUP BY c.id
                ),
                kw_per_cat AS (
                    SELECT jc.category_id, jk.keyword_id, COUNT(DISTINCT jk.job_id) AS n
                    FROM job_keywords jk
                    JOIN job_categories jc ON jc.job_id = jk.job_id
                    GROUP BY jc.category_id, jk.keyword_id
                )
                SELECT DISTINCT kw.keyword_id
                FROM kw_per_cat kw
                JOIN cat_totals ct ON ct.id = kw.category_id
                WHERE kw.n >= CEIL(ct.total * {threshold})
                """)
            .ToListAsync();

        return [..ids];
    }
}
