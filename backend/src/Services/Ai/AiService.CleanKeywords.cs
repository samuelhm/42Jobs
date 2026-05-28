using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<List<string>> CleanKeywordsAsync(
        List<string> keywords, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, defaultModelId, useReasoning, reasoningEffort) = await LoadPromptAsync("clean_keywords");
            var keywordsList = string.Join("\n", keywords.Select(k => $"- {k}"));
            var userPrompt = FillTemplate(userTemplate, new() { ["keywords"] = keywordsList });

            var resolved = await ResolveModelAsync(defaultModelId);
            var schema = LoadSchema("clean_keywords", resolved.Provider.ServiceName);
            var result = await CallWithRetryAsync(resolved, systemPrompt, userPrompt, schema, "clean_keywords", ct, useThinking: useReasoning, thinkingEffort: reasoningEffort);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                var errMsg = err.GetString();
                if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
                {
                    _logger.LogWarning("AI clean keywords error: {Error}", errMsg);
                    return [];
                }
            }

            var remove = new List<string>();
            if (result.TryGetProperty("remove", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                    remove.Add(item.GetString() ?? "");
            }

            return remove;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI clean keywords failed");
            return [];
        }
    }
}
