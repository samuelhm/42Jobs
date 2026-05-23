using src.Models.DTOs;

namespace src.Services;

public interface IAiService
{
    Task<(string relevante, string aptoJunior)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default);

    Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default);

    Task<(List<GithubProjectResult> projects, string error)> AnalyzeGithubProjectsAsync(
        string inputText, CancellationToken ct = default);

    Task<List<List<string>>> DedupKeywordsAsync(
        List<string> allKeywords, CancellationToken ct = default);

    Task<(List<LinkedInExperienceParsed> items, string? error)> ParseLinkedInExperienceAsync(
        string rawText, CancellationToken ct = default);

    Task<(List<LinkedInEducationParsed> items, string? error)> ParseLinkedInEducationAsync(
        string rawText, CancellationToken ct = default);
}
