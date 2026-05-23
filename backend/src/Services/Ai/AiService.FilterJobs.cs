using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(string relevant, string juniorFriendly)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema, defaultModelId) = await LoadPromptAsync("filter_jobs");
            var userPrompt = FillTemplate(userTemplate, new()
            {
                ["keyword"] = keyword,
                ["title"] = title,
                ["description"] = description ?? "No disponible"
            });

            var (provider, model, apiKey, isFreeTier) = await ResolveModelAsync(defaultModelId);
            var result = await CallWithRetryAsync(provider, systemPrompt, userPrompt, schema, model, apiKey, isFreeTier, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI filter error for '{Title}': {Error}", title, err.GetString());
                return ("yes", "yes");
            }

            var relevant = result.GetProperty("relevant").GetString() ?? "yes";
            var juniorFriendly = result.GetProperty("junior_friendly").GetString() ?? "yes";

            _logger.LogDebug("Filter for '{Title}': relevant={Relevant}, junior_friendly={JuniorFriendly}",
                title, relevant, juniorFriendly);

            return (relevant, juniorFriendly);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI filter failed for '{Title}', defaulting to pass", title);
            return ("yes", "yes");
        }
    }
}
