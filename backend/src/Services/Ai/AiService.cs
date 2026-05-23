using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using src.Data;
using src.Services.Ai.Providers;

namespace src.Services.Ai;

public partial class AiService : IAiService
{
    private readonly Dictionary<string, IAiProvider> _providers;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiService> _logger;

    public AiService(IEnumerable<IAiProvider> providers, IServiceScopeFactory scopeFactory, ILogger<AiService> logger)
    {
        _providers = providers.ToDictionary(p => p.ServiceName);
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private async Task<(string systemPrompt, string userPromptTemplate, JsonElement schema, int? defaultModelId)> LoadPromptAsync(
        string functionality)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var prompt = await db.AiPrompts
            .Include(p => p.Schema)
            .FirstOrDefaultAsync(p => p.Functionality == functionality && p.IsActive)
            ?? throw new InvalidOperationException($"No active prompt for functionality '{functionality}'");

        var schema = JsonDocument.Parse(prompt.Schema!.JsonSchema).RootElement;
        return (prompt.SystemPrompt, prompt.UserPromptTemplate, schema, prompt.DefaultModelId);
    }

    private async Task<(IAiProvider provider, string model, string? apiKey)> ResolveModelAsync(int? defaultModelId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var model = await db.AiModels
            .Include(m => m.AiService)
            .FirstOrDefaultAsync(m => m.Id == defaultModelId && m.IsActive && m.AiService.IsActive)
            ?? throw new InvalidOperationException($"No active model configured for this task. Set it in Admin > AI Prompts.");

        var provider = _providers.GetValueOrDefault(model.AiService.Name)
            ?? throw new InvalidOperationException($"No provider registered for service '{model.AiService.Name}'");

        return (provider, model.Name, model.AiService.ApiKey);
    }

    private static string FillTemplate(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}
