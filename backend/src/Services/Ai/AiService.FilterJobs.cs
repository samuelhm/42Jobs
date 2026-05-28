using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(string relevant, string juniorFriendly)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default)
    {
        var (systemPrompt, userTemplate, defaultModelId, useReasoning, reasoningEffort) = await LoadPromptAsync("filter_jobs");
        var userPrompt = FillTemplate(userTemplate, new()
        {
            ["keyword"] = keyword,
            ["title"] = title,
            ["description"] = description ?? "No disponible"
        });

        var resolved = await ResolveModelAsync(defaultModelId);
        var schema = LoadSchema("filter_jobs", resolved.Provider.ServiceName);
        var result = await CallWithRetryAsync(resolved, systemPrompt, userPrompt, schema,
            "filter_jobs", ct, useThinking: useReasoning, thinkingEffort: reasoningEffort);

        if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
        {
            var errMsg = err.GetString();
            if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
                throw new InvalidOperationException($"AI filter error for '{title}': {errMsg}");
        }

        var relevant = result.GetProperty("relevant").GetString() ?? "yes";
        var juniorFriendly = result.GetProperty("junior_friendly").GetString() ?? "yes";

        _logger.LogDebug("Filter for '{Title}': relevant={Relevant}, junior_friendly={JuniorFriendly}",
            title, relevant, juniorFriendly);

        return (relevant, juniorFriendly);
    }
}
