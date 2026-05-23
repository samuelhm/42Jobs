using System.Text;
using System.Text.Json;

namespace src.Services.Ai.Providers.Gemini;

public class GeminiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<GeminiProvider> _logger;
    private const string BaseUrl = "https://generativelanguage.googleapis.com";
    private const string EnvApiKey = "LLM_GOOGLE_API_KEY";

    public static string ServiceName => "Google";
    string IAiProvider.ServiceName => ServiceName;

    public GeminiProvider(IHttpClientFactory httpFactory, ILogger<GeminiProvider> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<JsonElement> CallAsync(
        string systemPrompt, string userPrompt, JsonElement schema, string model, string? apiKey, CancellationToken ct)
    {
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";
        var key = apiKey ?? Environment.GetEnvironmentVariable(EnvApiKey);

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = combinedPrompt } } }
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                response_schema = schema,
                temperature = 0.1
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);

        var url = $"{BaseUrl}/v1beta/models/{model}:generateContent";
        if (!string.IsNullOrEmpty(key))
            url += $"?key={key}";

        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString()!;

        return JsonDocument.Parse(text).RootElement;
    }
}
