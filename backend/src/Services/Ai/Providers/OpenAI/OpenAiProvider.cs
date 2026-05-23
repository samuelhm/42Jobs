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

        var requestBody = new
        {
            model,
            input = combinedPrompt,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "response",
                    strict = true,
                    schema = AdaptSchema(schema)
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await http.PostAsync($"{BaseUrl}/v1/responses", content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        var text = root
            .GetProperty("output")[0]
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()!;

        return JsonDocument.Parse(text).RootElement;
    }

    private static object AdaptSchema(JsonElement schema)
    {
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);
        WriteSchemaElement(writer, schema);
        writer.Flush();
        return System.Text.Json.JsonSerializer.Deserialize<object>(ms.ToArray())!;
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
                    var nextIsType = prop.NameEquals("type");
                    WriteSchemaElement(writer, prop.Value, nextIsType);
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
                if (isType)
                    val = val.ToLowerInvariant();
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
