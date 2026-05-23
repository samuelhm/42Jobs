using System.Text;
using System.Text.Json;

namespace src.Services.Ai.Providers.OpenAI;

public class OpenAiProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<OpenAiProvider> _logger;
    private const string BaseUrl = "https://api.openai.com";

    public static string ServiceName => "OpenAI";
    string IAiProvider.ServiceName => ServiceName;

    public OpenAiProvider(IHttpClientFactory httpFactory, ILogger<OpenAiProvider> logger)
    {
        _httpFactory = httpFactory;
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
            WriteSchemaElement(writer, schema);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var content = new StringContent(Encoding.UTF8.GetString(bodyStream.ToArray()), Encoding.UTF8, "application/json");
        var response = await http.PostAsync($"{BaseUrl}/v1/responses", content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
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

        return JsonDocument.Parse(text).RootElement;
    }

    private static void WriteSchemaElement(Utf8JsonWriter writer, JsonElement element, bool isType = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals("additionalProperties"))
                    {
                        writer.WriteBoolean("additionalProperties", false);
                        continue;
                    }
                    writer.WritePropertyName(prop.Name);
                    WriteSchemaElement(writer, prop.Value, prop.NameEquals("type"));
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteSchemaElement(writer, item, isType);
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                var val = element.GetString() ?? "";
                if (isType) val = val.ToLowerInvariant();
                writer.WriteStringValue(val);
                break;

            case JsonValueKind.Number:
                writer.WriteNumberValue(element.GetDecimal());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }
}
