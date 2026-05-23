using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<List<List<string>>> DedupKeywordsAsync(
        List<string> allKeywords, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema, defaultModelId) = await LoadPromptAsync("dedup_keywords");
            var keywordsList = string.Join("\n", allKeywords.Select(k => $"- {k}"));
            var userPrompt = FillTemplate(userTemplate, new() { ["keywords"] = keywordsList });

            var (provider, model, apiKey) = await ResolveModelAsync(defaultModelId);
            var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI dedup error: {Error}", err.GetString());
                return allKeywords.Select(k => new List<string> { k }).ToList();
            }

            var groups = new List<List<string>>();
            if (result.TryGetProperty("groups", out var arr))
            {
                foreach (var group in arr.EnumerateArray())
                {
                    var items = new List<string>();
                    foreach (var item in group.EnumerateArray())
                        items.Add(item.GetString() ?? "");
                    if (items.Count > 0) groups.Add(items);
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI dedup failed");
            return allKeywords.Select(k => new List<string> { k }).ToList();
        }
    }
}
