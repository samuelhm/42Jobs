using System.Text;
using System.Text.Json;

namespace src.Services.Ai.Providers.OpenAI;

public class OpenAiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AdminLogService _log;
    private readonly ILogger<OpenAiProvider> _logger;
    private const string BaseUrl = "https://api.openai.com";

    public static string ServiceName => "OpenAI";
    string IAiProvider.ServiceName => ServiceName;

    public OpenAiProvider(IHttpClientFactory httpFactory, AdminLogService log, ILogger<OpenAiProvider> logger)
    {
        _httpFactory = httpFactory;
        _log = log;
        _logger = logger;
    }

    public async Task<JsonElement> CallAsync(
        string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey, string functionality, CancellationToken ct, bool useThinking = false, string? thinkingEffort = null)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Set it in Admin > AI Services.");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(600);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        using var bodyStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bodyStream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WriteString("instructions", systemPrompt);
            writer.WriteString("input", userPrompt);
            writer.WriteStartObject("text");
            writer.WriteStartObject("format");
            writer.WriteString("type", "json_schema");
            writer.WriteString("name", "response");
            writer.WriteBoolean("strict", true);
            writer.WritePropertyName("schema");
            schema.WriteTo(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
            if (useThinking)
            {
                writer.WriteStartObject("reasoning");
                if (thinkingEffort is not null)
                    writer.WriteString("effort", thinkingEffort);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
        }

        var content = new StringContent(Encoding.UTF8.GetString(bodyStream.ToArray()), Encoding.UTF8, "application/json");

        await _log.LogAsync("OpenAI", functionality,
            new { system_prompt = systemPrompt, user_prompt = userPrompt, model, use_thinking = useThinking },
            model, "sent", correlationId);

        var response = await http.PostAsync($"{BaseUrl}/v1/responses", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            await _log.LogAsync("OpenAI", functionality,
                new { error = responseBody, status_code = (int)response.StatusCode },
                model, $"error:{(int)response.StatusCode}", correlationId);
            _logger.LogError("OpenAI error (HTTP {Status}): {Body}", (int)response.StatusCode,
                responseBody.Length > 800 ? responseBody[..800] : responseBody);
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = FindTextInOutput(root);
        if (text is null)
        {
            _logger.LogError("OpenAI response has no text content: {Body}",
                responseBody.Length > 800 ? responseBody[..800] : responseBody);
            await _log.LogAsync("OpenAI", functionality,
                new { error = "No text content in response output", body_preview = responseBody.Length > 500 ? responseBody[..500] : responseBody },
                model, "error: no text in output", correlationId);
            throw new InvalidOperationException("OpenAI response has no text content");
        }

        var result = JsonDocument.Parse(text).RootElement;
        await _log.LogAsync("OpenAI", functionality,
            result,
            model, "received:200", correlationId);

        return result;
    }

    private static string? FindTextInOutput(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output))
            return null;

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
                continue;

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var t))
                    return t.GetString();
            }
        }

        return null;
    }
}
