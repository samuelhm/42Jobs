using System.Text.Json;

namespace src.Services.Ai.Providers;

public interface IAiProvider
{
    string ServiceName { get; }
    Task<JsonElement> CallAsync(string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey, CancellationToken ct);
}
