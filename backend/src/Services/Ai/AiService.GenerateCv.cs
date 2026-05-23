using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<JsonElement> GenerateCvAsync(
        Dictionary<string, string> context, CancellationToken ct = default)
    {
        var (systemPrompt, userTemplate, schema, defaultModelId) = await LoadPromptAsync("cv_generation");
        var userPrompt = FillTemplate(userTemplate, context);

        var (provider, model, apiKey) = await ResolveModelAsync(defaultModelId);
        var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

        if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
        {
            _logger.LogWarning("AI CV generation error: {Error}", err.GetString());
            throw new InvalidOperationException(err.GetString() ?? "CV generation failed");
        }

        return result;
    }
}
