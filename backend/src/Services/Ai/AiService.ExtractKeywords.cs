using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema) = await LoadPromptAsync("extract_keywords");
            var userPrompt = FillTemplate(userTemplate, new() { ["text"] = text });

            var (provider, model, apiKey) = await ResolveDefaultAsync();
            var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI keyword extraction error: {Error}", err.GetString());
                return ([], "No identificado");
            }

            var skills = new List<string>();
            if (result.TryGetProperty("skills", out var skillsArray))
            {
                foreach (var skill in skillsArray.EnumerateArray())
                    skills.Add(skill.GetString() ?? string.Empty);
            }

            var companyType = "No identificado";
            if (result.TryGetProperty("tipo_empresa", out var tipoElement))
                companyType = tipoElement.GetString() ?? "No identificado";

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
