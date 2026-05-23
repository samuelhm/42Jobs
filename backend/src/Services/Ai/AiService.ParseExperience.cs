using System.Text.Json;
using src.Models.DTOs;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<LinkedInExperienceParsed> items, string? error)> ParseLinkedInExperienceAsync(
        string rawText, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema, defaultModelId) = await LoadPromptAsync("parse_experience");
            var userPrompt = FillTemplate(userTemplate, new() { ["raw_text"] = rawText });

            var (provider, model, apiKey) = await ResolveModelAsync(defaultModelId);
            var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI experience parsing error: {Error}", err.GetString());
                return ([], err.GetString());
            }

            var items = new List<LinkedInExperienceParsed>();
            if (result.TryGetProperty("experiences", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    items.Add(new LinkedInExperienceParsed
                    {
                        Company = item.GetProperty("company").GetString() ?? "",
                        Position = GetNullableString(item, "position"),
                        StartDate = GetNullableString(item, "start_date"),
                        EndDate = GetNullableString(item, "end_date"),
                        Description = GetNullableString(item, "description")
                    });
                }
            }

            return (items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI experience parsing failed");
            return ([], ex.Message);
        }
    }

    private static string? GetNullableString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var val) && val.ValueKind != JsonValueKind.Null
            ? val.GetString()
            : null;
    }
}
