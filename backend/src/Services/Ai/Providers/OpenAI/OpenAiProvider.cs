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
        string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey, CancellationToken ct)
    {
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key not configured. Set it in Admin > AI Services.");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        using var bodyStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bodyStream))
        {
            writer.WriteStartObject();
            writer.WriteString("model", model);
            writer.WriteString("input", combinedPrompt);
            writer.WriteStartObject("text");
            writer.WriteStartObject("format");
            writer.WriteString("type", "json_schema");
            writer.WriteString("name", "response");
            writer.WriteBoolean("strict", true);
            writer.WritePropertyName("schema");
            schema.WriteTo(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var content = new StringContent(Encoding.UTF8.GetString(bodyStream.ToArray()), Encoding.UTF8, "application/json");

        await _log.LogAsync("OpenAI", "llm:call",
            new { system_prompt = systemPrompt, user_prompt = userPrompt, model },
            model, "sent");

        var response = await http.PostAsync($"{BaseUrl}/v1/responses", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            await _log.LogAsync("OpenAI", "llm:call",
                new { error = responseBody, status_code = (int)response.StatusCode },
                model, $"error:{(int)response.StatusCode}");
            _logger.LogError("OpenAI error (HTTP {Status}): {Body}", (int)response.StatusCode,
                responseBody.Length > 800 ? responseBody[..800] : responseBody);
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("output")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!;

        var result = JsonDocument.Parse(text).RootElement;
        await _log.LogAsync("OpenAI", "llm:call",
            result,
            model, "received:200");

        return result;
    }
}
