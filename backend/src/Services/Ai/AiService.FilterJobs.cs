using System.Text.Json;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(string relevante, string aptoJunior)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema) = await LoadPromptAsync("filter_jobs");
            var userPrompt = FillTemplate(userTemplate, new()
            {
                ["keyword"] = keyword,
                ["title"] = title,
                ["description"] = description ?? "No disponible"
            });

            var (provider, model, apiKey) = await ResolveDefaultAsync();
            var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI filter error for '{Title}': {Error}", title, err.GetString());
                return ("si", "si");
            }

            var relevante = result.GetProperty("relevante").GetString() ?? "si";
            var aptoJunior = result.GetProperty("apto_junior").GetString() ?? "si";

            _logger.LogDebug("Filter for '{Title}': relevante={Relevante}, apto_junior={AptoJunior}",
                title, relevante, aptoJunior);

            return (relevante, aptoJunior);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI filter failed for '{Title}', defaulting to pass", title);
            return ("si", "si");
        }
    }
}
