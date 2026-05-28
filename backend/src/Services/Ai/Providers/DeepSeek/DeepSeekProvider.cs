using System.Text;
using System.Text.Json;

namespace src.Services.Ai.Providers.DeepSeek;

public class DeepSeekProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AdminLogService _log;
    private readonly ILogger<DeepSeekProvider> _logger;
    private const string BaseUrl = "https://api.deepseek.com";

    public static string ServiceName => "DeepSeek";
    string IAiProvider.ServiceName => ServiceName;

    public DeepSeekProvider(IHttpClientFactory httpFactory, AdminLogService log, ILogger<DeepSeekProvider> logger)
    {
        _httpFactory = httpFactory;
        _log = log;
        _logger = logger;
    }

    public async Task<JsonElement> CallAsync(
        string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey,
        string functionality, CancellationToken ct, bool useThinking = false, string? thinkingEffort = null)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("DeepSeek API key not configured. Set it in Admin > AI Services.");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(600);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var schemaJson = schema.GetRawText();

        var enrichedSystemPrompt = systemPrompt + "\n\nYou must respond with a JSON object matching this exact structure:\n" + schemaJson;

        var messages = new[]
        {
            new { role = "system", content = enrichedSystemPrompt },
            new { role = "user", content = userPrompt }
        };

        using var bodyStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bodyStream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WritePropertyName("messages");
            JsonSerializer.Serialize(writer, messages);
            writer.WriteNumber("max_tokens", 32768);
            writer.WriteNumber("temperature", 0.1);

            writer.WriteStartObject("response_format");
            writer.WriteString("type", "json_object");
            writer.WriteEndObject();

            if (useThinking)
            {
                writer.WriteStartObject("thinking");
                writer.WriteString("type", "enabled");
                writer.WriteEndObject();
                if (thinkingEffort is not null)
                    writer.WriteString("reasoning_effort", thinkingEffort);
            }

            writer.WriteEndObject();
        }

        var content = new StringContent(Encoding.UTF8.GetString(bodyStream.ToArray()), Encoding.UTF8, "application/json");

        await _log.LogAsync("DeepSeek", functionality,
            new { system_prompt = systemPrompt, user_prompt = userPrompt, schema, model, use_thinking = useThinking },
            model, "sent", correlationId);

        var response = await http.PostAsync($"{BaseUrl}/chat/completions", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            await _log.LogAsync("DeepSeek", functionality,
                new { error = responseBody, status_code = (int)response.StatusCode },
                model, $"error:{(int)response.StatusCode}", correlationId);
            _logger.LogError("DeepSeek error (HTTP {Status}): {Body}", (int)response.StatusCode,
                responseBody.Length > 800 ? responseBody[..800] : responseBody);
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;

        var result = JsonDocument.Parse(text).RootElement;
        await _log.LogAsync("DeepSeek", functionality,
            result,
            model, "received:200", correlationId);

        return result;
    }
}
