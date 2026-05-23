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
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("DeepSeek API key not configured. Set it in Admin > AI Services.");

        // Build the request in your provider's format.
        // If your API is OpenAI-compatible (Chat Completions), use:
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
                    schema = schema  // JsonElement — no adaptation needed for standard JSON Schema
                }
            },
            temperature = 0.1
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(120);
        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

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

### Schema files

Schemas are **file-based**, stored in `backend/src/Services/Ai/Schemas/`. Each functionality has one file per provider:

```
Schemas/
  filter_jobs.openai.json     ← with "additionalProperties": false (OpenAI strict mode)
  filter_jobs.google.json     ← without additionalProperties (Gemini rejects it)
  extract_keywords.openai.json
  extract_keywords.google.json
  ...
```

The `AiService.LoadSchema(functionality, providerServiceName)` method resolves the file at runtime. The provider receives the `JsonElement` as-is — **no runtime adaptation is needed** because each provider already has its own schema variant.

If your provider uses a non-standard schema format, add a new schema file for it under `Schemas/` with the naming pattern `{functionality}.{provider}.json` (provider name lowercase).

### API key

API keys are managed exclusively through the **Admin panel** (`ai_services.api_key` in the database). The `apiKey` parameter passed to `CallAsync` comes from the DB. There is no env var fallback.

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

Use the Admin panel (recommended) or insert directly via SQL:

```sql
-- Insert the service
INSERT INTO ai_services (name, is_free_tier, api_key) VALUES
    ('DeepSeek', FALSE, 'sk-your-key-here')
ON CONFLICT (name) DO NOTHING;

-- Insert its models
INSERT INTO ai_models (ai_service_id, name) VALUES
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-chat'),
    ((SELECT id FROM ai_services WHERE name = 'DeepSeek'), 'deepseek-reasoner')
ON CONFLICT (ai_service_id, name) DO NOTHING;
```

- `is_free_tier`: enable if this provider has rate limits (adds pre-call delay + exponential backoff on 429).
- `api_key`: can be `NULL` and set later via the admin panel.
- To make a model the default for a prompt, set `default_model_id` in `ai_prompts` via Admin > AI Prompts.

## 4. Verify

Restart the backend:

```bash
make dev-restart
```

Test through the UI or API. Assign the new model to a prompt in Admin > AI Prompts to route calls through your provider.

## What NOT to do

- **Do not** modify `AiService` — it resolves providers generically via `IAiProvider.ServiceName`.
- **Do not** add your provider to controllers — controllers only know `IAiService`.
- **Do not** hardcode prompts in your provider — prompts live in `ai_prompts`.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `No active prompt for functionality X` | No prompt with `is_active = TRUE` for that functionality. Check Admin > AI Prompts. |
| `No active model configured for this task` | The prompt has no `default_model_id`, or the model is inactive. Set it in Admin > AI Prompts. |
| `No provider registered for service X` | The `ServiceName` in your provider doesn't match the `ai_services.name` in the DB. They must be identical. |
| Provider not called | Check `ai_models.is_active` and `ai_services.is_active` in the DB. |
| `401 Unauthorized` | `api_key` is NULL in `ai_services`. Set it in Admin > AI Services. |
| `Rate limit (429) but API key is not marked as free tier` | The provider is hitting rate limits but `is_free_tier = FALSE`. Enable free tier in Admin > AI Services to add retry logic. |
