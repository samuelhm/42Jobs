# How to add a new AI provider

42jobs uses a pluggable AI architecture. The backend never calls a specific provider directly. Instead, controllers inject `IAiService`, which resolves the active provider from the database at runtime.

To add a new provider (e.g., DeepSeek, Anthropic, Mistral), follow these steps.

## 1. Create the provider class

Create a new folder under `backend/src/Services/Ai/Providers/`:

```
Providers/DeepSeek/
    DeepSeekProvider.cs
```

Implement the `IAiProvider` interface:

```csharp
using System.Text;
using System.Text.Json;
using src.Services.Ai.Providers;

namespace src.Services.Ai.Providers.DeepSeek;

public class DeepSeekProvider : IAiProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<DeepSeekProvider> _logger;

    public static string ServiceName => "DeepSeek";
    string IAiProvider.ServiceName => ServiceName;

    // Name of the env var that holds the default API key
    private const string EnvApiKey = "LLM_DEEPSEEK_API_KEY";

    public DeepSeekProvider(IHttpClientFactory httpFactory, ILogger<DeepSeekProvider> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<JsonElement> CallAsync(
        string systemPrompt,
        string userPrompt,
        JsonElement schema,
        string model,
        string? apiKey,
        CancellationToken ct)
    {
        // Use DB key if available, fall back to env var
        var key = apiKey ?? Environment.GetEnvironmentVariable(EnvApiKey);

        // Build the request in your provider's format.
        // Most APIs use an OpenAI-compatible chat format.
        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userPrompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "response",
                    strict = true,
                    schema = schema  // adapt if your API uses a different format
                }
            },
            temperature = 0.1
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

        var response = await http.PostAsync("https://api.deepseek.com/v1/chat/completions", content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(body);

        // Extract the JSON text from your provider's response format
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;

        return JsonDocument.Parse(text).RootElement;
    }
}
```

**Important:** `ServiceName` must match exactly what you will insert into the `ai_services` table (step 2).

## 2. Register the provider in DI

In `backend/src/Program.cs`, add one line:

```csharp
using src.Services.Ai.Providers.DeepSeek;  // add at the top

// Add with the other providers:
builder.Services.AddSingleton<IAiProvider, GeminiProvider>();
builder.Services.AddSingleton<IAiProvider, OpenAiProvider>();
builder.Services.AddSingleton<IAiProvider, DeepSeekProvider>();  // <-- add this
```

The `AiService` picks it up automatically via `IEnumerable<IAiProvider>` — no other code changes needed in the service layer.

## 3. Add the provider and models to the database

The seed file is at `database/migrations/022-seed-ai.sql` (or you can create a new migration for existing databases):

```sql
-- Insert the service
INSERT INTO ai_services (name, base_url, api_key) VALUES
    ('DeepSeek', 'https://api.deepseek.com/', NULL)
ON CONFLICT (name) DO NOTHING;

-- Insert its models
INSERT INTO ai_models (ai_service_id, name, is_default) VALUES
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-chat', FALSE),
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-reasoner', FALSE)
ON CONFLICT (ai_service_id, name) DO NOTHING;
```

- `api_key` can be `NULL` — the provider will fall back to the environment variable (`LLM_DEEPSEEK_API_KEY`).
- If you want this provider to be the default, set `is_default = TRUE` on one of its models. Only one model across all providers should have `is_default = TRUE`.

## 4. Set the API key

Add to your `.env` file:

```bash
LLM_DEEPSEEK_API_KEY=sk-xxxxxxxx
```

If you set `api_key` in the `ai_services` table, that value takes priority over the env var.

## 5. Verify

Restart the backend:

```bash
make dev-restart
```

Test a prompt through the API. If the new model is the default, all AI calls will now go through your provider. If not, you can make it the default or use it programmatically via a future admin panel.

## What NOT to do

- **Do not** modify `AiService` — it resolves providers generically via `IAiProvider.ServiceName`.
- **Do not** add your provider to controllers — controllers only know `IAiService`.
- **Do not** hardcode prompts in your provider — prompts live in `ai_prompts`.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `No default AI model configured` | No model has `is_default = TRUE`. Run the seed INSERT or set one manually. |
| `No provider registered for service X` | The `ServiceName` in your provider doesn't match the `ai_services.name` in the DB. They must be identical. |
| Provider not called | Check `ai_models.is_default` and `ai_models.is_active` for your model. |
| `401 Unauthorized` | API key is missing in both the DB and the env var. |
