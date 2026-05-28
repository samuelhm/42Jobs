using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(JsonElement result, string modelName)> GenerateCvAsync(
        Dictionary<string, string> context, CancellationToken ct = default)
    {
        var (systemPrompt, userTemplate, defaultModelId, useReasoning, reasoningEffort) = await LoadPromptAsync("cv_generation");
        var userPrompt = FillTemplate(userTemplate, context);

        var resolved = await ResolveModelAsync(defaultModelId);
        var schema = LoadSchema("cv_generation", resolved.Provider.ServiceName);
        var result = await CallWithRetryAsync(resolved, systemPrompt, userPrompt, schema, "cv_generation", ct, useThinking: useReasoning, thinkingEffort: reasoningEffort);

        if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
        {
            var errMsg = err.GetString();
            if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
            {
                _logger.LogWarning("AI CV generation error: {Error}", errMsg);
                throw new InvalidOperationException(errMsg ?? "CV generation failed");
            }
        }

        return (result, resolved.Name);
    }
}
