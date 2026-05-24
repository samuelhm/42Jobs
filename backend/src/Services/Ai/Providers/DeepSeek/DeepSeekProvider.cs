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
        CancellationToken ct, bool useThinking = false)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("DeepSeek API key not configured. Set it in Admin > AI Services.");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var schemaJson = schema.GetRawText();

        var messages = new[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };

        using var bodyStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bodyStream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WritePropertyName("messages");
            JsonSerializer.Serialize(writer, messages);
            writer.WriteNumber("max_tokens", 8000);
            writer.WriteNumber("temperature", 0.1);

            writer.WriteStartObject("response_format");
            writer.WriteString("type", "json_object");
            writer.WriteEndObject();

            if (useThinking)
            {
                writer.WriteStartObject("thinking");
                writer.WriteString("type", "enabled");
                writer.WriteEndObject();
                writer.WriteString("reasoning_effort", "high");
            }

            writer.WriteEndObject();
        }

        var content = new StringContent(Encoding.UTF8.GetString(bodyStream.ToArray()), Encoding.UTF8, "application/json");

        await _log.LogAsync("DeepSeek", "llm:call",
            new { system_prompt = systemPrompt, user_prompt = userPrompt, schema = schemaJson, model, use_thinking = useThinking },
            model, "sent");

        var response = await http.PostAsync($"{BaseUrl}/chat/completions", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            await _log.LogAsync("DeepSeek", "llm:call",
                new { error = responseBody, status_code = (int)response.StatusCode },
                model, $"error:{(int)response.StatusCode}");
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
        await _log.LogAsync("DeepSeek", "llm:call",
            result,
            model, "received:200");

        return result;
    }
}
