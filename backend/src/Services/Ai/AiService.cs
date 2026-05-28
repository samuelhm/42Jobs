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
    private readonly EncryptionService _encryption;
    private readonly ILogger<AiService> _logger;
    private readonly IWebHostEnvironment _env;

    public AiService(IEnumerable<IAiProvider> providers, IServiceScopeFactory scopeFactory, EncryptionService encryption, ILogger<AiService> logger, IWebHostEnvironment env)
    {
        _providers = providers.ToDictionary(p => p.ServiceName);
        _scopeFactory = scopeFactory;
        _encryption = encryption;
        _logger = logger;
        _env = env;
    }

    private async Task<(string systemPrompt, string userPromptTemplate, int? defaultModelId)> LoadPromptAsync(
        string functionality)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var prompt = await db.AiPrompts
            .FirstOrDefaultAsync(p => p.Functionality == functionality && p.IsActive)
            ?? throw new InvalidOperationException($"No active prompt for functionality '{functionality}'");

        return (prompt.SystemPrompt, prompt.UserPromptTemplate, prompt.DefaultModelId);
    }

    private async Task<(IAiProvider provider, string model, string? apiKey, bool isFreeTier)> ResolveModelAsync(int? defaultModelId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var model = await db.AiModels
            .Include(m => m.AiService)
            .FirstOrDefaultAsync(m => m.Id == defaultModelId && m.IsActive && m.AiService.IsActive)
            ?? throw new InvalidOperationException($"No active model configured for this task. Set it in Admin > AI Prompts.");

        var provider = _providers.GetValueOrDefault(model.AiService.Name)
            ?? throw new InvalidOperationException($"No provider registered for service '{model.AiService.Name}'");

        return (provider, model.Name, _encryption.Decrypt(model.AiService.ApiKey), model.AiService.IsFreeTier);
    }

    private JsonElement LoadSchema(string functionality, string serviceName)
    {
        var providerKey = serviceName.ToLowerInvariant();
        var path = Path.Combine(_env.ContentRootPath, "Services", "Ai", "Schemas", $"{functionality}.{providerKey}.json");
        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"Schema file not found: {path}. Expected schema per provider at Services/Ai/Schemas/{{functionality}}.{{provider}}.json");
        var json = File.ReadAllText(path);
        return JsonDocument.Parse(json).RootElement;
    }

    private async Task<JsonElement> CallWithRetryAsync(
        IAiProvider provider, string systemPrompt, string userPrompt,
        JsonElement schema, string model, string? apiKey, string functionality, bool isFreeTier, CancellationToken ct, bool useThinking = false, string? thinkingEffort = null)
    {
        if (isFreeTier)
            await Task.Delay(6000 + Random.Shared.Next(1500), ct);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                return await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, functionality, ct, useThinking, thinkingEffort);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (!isFreeTier)
                    throw new InvalidOperationException(
                        "Rate limit (429) detected but API key is not marked as free tier. " +
                        "Go to Admin > AI Services and enable 'Free tier' for this provider. " +
                        "The process will be slower but will respect rate limits.", ex);

                if (attempt == 5) throw;
                var delay = (int)Math.Pow(2, attempt) * 1000 + Random.Shared.Next(800);
                _logger.LogWarning("Free tier rate limit (429), retry {Attempt}/5 in {Delay}ms", attempt, delay);
                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException("Unreachable");
    }

    private static string FillTemplate(string template, Dictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
            result = result.Replace($"{{{{{key}}}}}", value);
        return result;
    }
}
