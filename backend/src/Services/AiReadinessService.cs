using Microsoft.EntityFrameworkCore;
using src.Data;

namespace src.Services;

public interface IAiReadinessService
{
    Task<List<string>> CheckAsync(string functionality, CancellationToken ct = default);
}

public class AiReadinessService : IAiReadinessService
{
    private readonly AppDbContext _db;

    public AiReadinessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<string>> CheckAsync(string functionality, CancellationToken ct = default)
    {
        var errors = new List<string>();

        var prompt = await _db.AiPrompts
            .FirstOrDefaultAsync(p => p.Functionality == functionality && p.IsActive, ct);

        if (prompt is null)
        {
            errors.Add($"'{functionality}': no active prompt found");
            return errors;
        }

        if (prompt.DefaultModelId is null)
        {
            errors.Add($"'{functionality}': no model assigned (Admin > AI Prompts)");
            return errors;
        }

        var model = await _db.AiModels
            .Include(m => m.AiService)
            .FirstOrDefaultAsync(m => m.Id == prompt.DefaultModelId && m.IsActive && m.AiService.IsActive, ct);

        if (model is null)
        {
            errors.Add($"'{functionality}': assigned model (id={prompt.DefaultModelId}) is inactive or missing");
            return errors;
        }

        if (string.IsNullOrEmpty(model.AiService.ApiKey))
        {
            errors.Add($"'{functionality}': service '{model.AiService.Name}' has no API key (Admin > AI Services)");
        }

        return errors;
    }
}
