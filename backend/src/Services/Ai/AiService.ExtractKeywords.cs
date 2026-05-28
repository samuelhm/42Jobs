using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default)
    {
        var (systemPrompt, userTemplate, defaultModelId) = await LoadPromptAsync("extract_keywords");
        var userPrompt = FillTemplate(userTemplate, new() { ["text"] = text });

        var resolved = await ResolveModelAsync(defaultModelId);
        var schema = LoadSchema("extract_keywords", resolved.Provider.ServiceName);
        var result = await CallWithRetryAsync(resolved, systemPrompt, userPrompt, schema,
            "extract_keywords", ct, useThinking: true);

        if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
        {
            var errMsg = err.GetString();
            if (!string.IsNullOrEmpty(errMsg) && errMsg != "null")
                throw new InvalidOperationException($"AI keyword extraction error: {errMsg}");
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
}
