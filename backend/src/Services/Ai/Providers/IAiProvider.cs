using System.Text.Json;

namespace src.Services.Ai.Providers;

public interface IAiProvider
{
    string ServiceName { get; }
    Task<JsonElement> CallAsync(
        string systemPrompt, string userPrompt,
        JsonElement schema, string model, string? apiKey,
        string functionality, CancellationToken ct,
        bool useThinking = false, string? thinkingEffort = null);
}
