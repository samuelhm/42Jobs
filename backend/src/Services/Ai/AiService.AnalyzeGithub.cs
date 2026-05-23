using System.Text.Json;
using src.Models.DTOs;

namespace src.Services.Ai;

public partial class AiService
{
    public async Task<(List<GithubProjectResult> projects, string error)> AnalyzeGithubProjectsAsync(
        string inputText, CancellationToken ct = default)
    {
        try
        {
            var (systemPrompt, userTemplate, schema) = await LoadPromptAsync("analyze_github");
            var userPrompt = FillTemplate(userTemplate, new() { ["input"] = inputText });

            var (provider, model, apiKey) = await ResolveDefaultAsync();
            var result = await provider.CallAsync(systemPrompt, userPrompt, schema, model, apiKey, ct);

            if (result.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                _logger.LogWarning("AI GitHub analysis error: {Error}", err.GetString());
                return ([], err.GetString() ?? "Unknown error");
            }

            var projects = new List<GithubProjectResult>();
            if (result.TryGetProperty("projects", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var proj = new GithubProjectResult();
                    if (item.TryGetProperty("name", out var n)) proj.Name = n.GetString() ?? "";
                    if (item.TryGetProperty("description", out var d)) proj.Description = d.GetString() ?? "";
                    if (item.TryGetProperty("type", out var t)) proj.Type = t.GetString() ?? "personal";
                    if (item.TryGetProperty("keywords", out var kwArr))
                    {
                        foreach (var kw in kwArr.EnumerateArray())
                            proj.Keywords.Add(kw.GetString() ?? "");
                    }
                    projects.Add(proj);
                }
            }

            return (projects, "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI GitHub analysis failed");
            return ([], ex.Message);
        }
    }
}
