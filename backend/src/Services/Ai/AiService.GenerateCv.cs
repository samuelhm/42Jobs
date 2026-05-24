using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(JsonElement result, string modelName)> GenerateCvAsync(
        Dictionary<string, string> context, CancellationToken ct = default)
    {
        var (systemPrompt, userTemplate, defaultModelId) = await LoadPromptAsync("cv_generation");
        var userPrompt = FillTemplate(userTemplate, context);

        var (provider, model, apiKey, isFreeTier) = await ResolveModelAsync(defaultModelId);
        var schema = LoadSchema("cv_generation", provider.ServiceName);
        var result = await CallWithRetryAsync(provider, systemPrompt, userPrompt, schema, model, apiKey, isFreeTier, ct, useThinking: true);

        if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
        {
            var errMsg = err.GetString();
            if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
            {
                _logger.LogWarning("AI CV generation error: {Error}", errMsg);
                throw new InvalidOperationException(errMsg ?? "CV generation failed");
            }
        }

        return (result, model);
    }
}
