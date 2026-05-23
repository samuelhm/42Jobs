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

    private async Task<(string systemPrompt, string userPromptTemplate, JsonElement schema)> LoadPromptAsync(
        string functionality)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var prompt = await db.AiPrompts
            .Include(p => p.Schema)
            .FirstOrDefaultAsync(p => p.Functionality == functionality && p.IsActive)
            ?? throw new InvalidOperationException($"No active prompt for functionality '{functionality}'");

        var schema = JsonDocument.Parse(prompt.Schema!.JsonSchema).RootElement;
        return (prompt.SystemPrompt, prompt.UserPromptTemplate, schema);
    }

    private async Task<(IAiProvider provider, string model, string? apiKey)> ResolveDefaultAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var defaultModel = await db.AiModels
            .Include(m => m.AiService)
            .FirstOrDefaultAsync(m => m.IsDefault && m.IsActive && m.AiService.IsActive)
            ?? throw new InvalidOperationException("No default AI model configured");

        var provider = _providers.GetValueOrDefault(defaultModel.AiService.Name)
            ?? throw new InvalidOperationException($"No provider registered for service '{defaultModel.AiService.Name}'");

        return (provider, defaultModel.Name, defaultModel.AiService.ApiKey);
    }

    private static string FillTemplate(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}
