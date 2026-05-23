using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema, defaultModelId) = await LoadPromptAsync("extract_keywords");
            var userPrompt = FillTemplate(userTemplate, new() { ["text"] = text });

            var (provider, model, apiKey, isFreeTier) = await ResolveModelAsync(defaultModelId);
            var result = await CallWithRetryAsync(provider, systemPrompt, userPrompt, schema, model, apiKey, isFreeTier, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI keyword extraction error: {Error}", err.GetString());
                return ([], "Not identified");
            }

            var skills = new List<string>();
            if (result.TryGetProperty("skills", out var skillsArray))
            {
                foreach (var skill in skillsArray.EnumerateArray())
                    skills.Add(skill.GetString() ?? string.Empty);
            }

            var companyType = "Not identified";
            if (result.TryGetProperty("company_type", out var tipoElement))
                companyType = tipoElement.GetString() ?? "Not identified";

            _logger.LogDebug("Extracted {Count} keywords, company type: {Type}", skills.Count, companyType);

            return (skills, companyType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Keyword extraction AI call failed");
            return ([], "No identificado");
        }
    }
}
