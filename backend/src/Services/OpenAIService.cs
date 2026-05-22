using System.Text;
using System.Text.Json;
using src.Models.DTOs;

namespace src.Services;

public class OpenAIService
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient http, ILogger<OpenAIService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(List<LinkedInExperienceParsed> items, string? error)> ParseExperienceAsync(
        string rawText, CancellationToken ct = default)
    {
        var prompt = $@"Extrae experiencias laborales a JSON. La linea de fechas SIEMPRE tiene este formato exacto: 'mes. año - mes. año · X años/meses'.

Ejemplo de linea de fechas: 'sept. 2023 - ene. 2024 · 5 meses'
→ start_date: '2023-09-01', end_date: '2024-01-01'

IGNORA la parte '· X años/meses'. SOLO extrae las dos fechas de esa linea.
Meses: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Campos: company, position, start_date, end_date, description

{rawText}";

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = "Eres un parser JSON. Responde solo con JSON válido, sin markdown ni explicaciones." },
                new { role = "user", content = prompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "experiences",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        description = "List of work experiences",
                        properties = new
                        {
                            experiences = new
                            {
                                type = "array",
                                description = "Array of work experience entries",
                                items = new
                                {
                                    type = "object",
                                    description = "A single work experience",
                                    properties = new
                                    {
                                        company = new { type = "string", description = "Company name" },
                                        position = new { type = "string", description = "Job title or position" },
                                        start_date = new { type = "string", description = "Start date as YYYY-MM-DD" },
                                        end_date = new { type = "string", description = "End date as YYYY-MM-DD" },
                                        description = new { type = "string", description = "Full job description text" }
                                    },
                                    required = new[] { "company" },
                                    additionalProperties = false
                                }
                            }
                        },
                        required = new[] { "experiences" },
                        additionalProperties = false
                    }
                }
            },
            temperature = 0.1,
            max_tokens = 4096
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/v1/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()!;

            using var resultDoc = JsonDocument.Parse(text);
            var root = resultDoc.RootElement;

            var items = new List<LinkedInExperienceParsed>();
            if (root.TryGetProperty("experiences", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    items.Add(new LinkedInExperienceParsed
                    {
                        Company = item.GetProperty("company").GetString() ?? "",
                        Position = item.TryGetProperty("position", out var p) ? p.GetString() : null,
                        StartDate = item.TryGetProperty("start_date", out var sd) ? sd.GetString() : null,
                        EndDate = item.TryGetProperty("end_date", out var ed) ? ed.GetString() : null,
                        Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                    });
                }
            }

            return (items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI experience parsing failed");
            return ([], ex.Message);
        }
    }
}
