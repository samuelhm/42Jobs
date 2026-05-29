using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace src.Controllers;

public partial class AdminController
{
    [HttpPost("dedup-known")]
    public async Task<IActionResult> DedupKnown()
    {
        var mappings = await _db.BlockedKeywords
            .Where(b => b.RedirectTo != null)
            .ToListAsync();

        if (mappings.Count == 0)
            return Ok(new { message = "No known dedup mappings found", merged = 0 });

        var merged = 0;
        var warnings = new List<string>();

        foreach (var mapping in mappings)
        {
            var dup = await _db.Keywords
                .FirstOrDefaultAsync(k => k.Name.ToLower() == mapping.Name.ToLower());

            if (dup is null) continue;

            var keep = await _db.Keywords.FindAsync(mapping.RedirectTo!.Value);
            if (keep is null)
            {
                warnings.Add($"Target keyword id={mapping.RedirectTo} not found for '{mapping.Name}'");
                continue;
            }

            if (keep.Id == dup.Id) continue;

            try
            {
                await MergeKeywordAsync(keep.Id, dup.Id);
                merged++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to merge '{dup.Name}' into '{keep.Name}': {ex.Message}");
            }
        }

        _ = _adminLog.LogAsync(
            actor: "admin",
            action: "dedup_known",
            payload1: new { merged, mappings = mappings.Select(m => new { m.Name, m.RedirectTo }).ToList() },
            payload2: warnings.Count > 0 ? string.Join("; ", warnings) : null,
            payload3: null);

        return Ok(new
        {
            message = $"Merged {merged} keywords using known dedup mappings",
            merged,
            warnings = warnings.Count > 0 ? warnings : null
        });
    }
}
