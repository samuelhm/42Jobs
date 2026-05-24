using System.Text.Json;
using src.Models.DTOs;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<LinkedInEducationParsed> items, string? error)> ParseLinkedInEducationAsync(
        string rawText, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, defaultModelId) = await LoadPromptAsync("parse_education");
            var userPrompt = FillTemplate(userTemplate, new() { ["raw_text"] = rawText });

            var (provider, model, apiKey, isFreeTier) = await ResolveModelAsync(defaultModelId);
            var schema = LoadSchema("parse_education", provider.ServiceName);
            var result = await CallWithRetryAsync(provider, systemPrompt, userPrompt, schema, model, apiKey, "parse_education", isFreeTier, ct, useThinking: false);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                var errMsg = err.GetString();
                if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
                {
                    _logger.LogWarning("AI education parsing error: {Error}", errMsg);
                    return ([], errMsg);
                }
            }

            var items = new List<LinkedInEducationParsed>();
            if (result.TryGetProperty("education", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    items.Add(new LinkedInEducationParsed
                    {
                        Degree = item.GetProperty("degree").GetString() ?? "",
                        Institution = GetNullableString(item, "institution"),
                        StartYear = item.TryGetProperty("start_year", out var sy) && sy.ValueKind == JsonValueKind.Number ? (int?)sy.GetInt32() : null,
                        EndYear = item.TryGetProperty("end_year", out var ey) && ey.ValueKind == JsonValueKind.Number ? (int?)ey.GetInt32() : null
                    });
                }
            }

            return (items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI education parsing failed");
            return ([], ex.Message);
        }
    }
}
