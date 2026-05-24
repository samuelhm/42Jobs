using System.Text;
using System.Text.Json;

namespace src.Services.Ai.Providers.Gemini;

public class GeminiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AdminLogService _log;
    private readonly ILogger<GeminiProvider> _logger;
    private const string BaseUrl = "https://generativelanguage.googleapis.com";

    public static string ServiceName => "Google";
    string IAiProvider.ServiceName => ServiceName;

    public GeminiProvider(IHttpClientFactory httpFactory, AdminLogService log, ILogger<GeminiProvider> logger)
    {
        _httpFactory = httpFactory;
        _log = log;
        _logger = logger;
    }

    public async Task<JsonElement> CallAsync(
        string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey, CancellationToken ct, bool useThinking = false)
    {
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Gemini API key not configured. Set it in Admin > AI Services.");

        var generationConfig = new Dictionary<string, object>
        {
            ["response_mime_type"] = "application/json",
            ["response_schema"] = schema,
            ["temperature"] = 0.1
        };
        if (useThinking)
            generationConfig["thinkingConfig"] = new { thinkingLevel = "HIGH" };

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = combinedPrompt } } }
            },
            generationConfig
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(600);

        var url = $"{BaseUrl}/v1beta/models/{model}:generateContent";
        if (!string.IsNullOrEmpty(apiKey))
            url += $"?key={apiKey}";

        await _log.LogAsync("Gemini", "llm:call",
            new { system_prompt = systemPrompt, user_prompt = userPrompt, model, use_thinking = useThinking },
            model, "sent");

        var response = await http.PostAsync(url, content, ct);

        var responseBody = string.Empty;
        if (response.IsSuccessStatusCode)
            responseBody = await response.Content.ReadAsStringAsync(ct);
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            await _log.LogAsync("Gemini", "llm:call",
                new { error = errorBody, status_code = (int)response.StatusCode },
                model, $"error:{(int)response.StatusCode}");
            _logger.LogError("Gemini HTTP {Status}: {Error}", (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()!;

        var result = JsonDocument.Parse(text).RootElement;
        await _log.LogAsync("Gemini", "llm:call",
            result,
            model, "received:200");

        return result;
    }
}
