using System.Net.Http.Headers;
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
        var input = $@"Extrae experiencias laborales a JSON. La linea de fechas SIEMPRE tiene este formato exacto: 'mes. año - mes. año · X años/meses'.

Ejemplo de linea de fechas: 'sept. 2023 - ene. 2024 · 5 meses'
-> start_date: '2023-09-01', end_date: '2024-01-01'

IGNORA la parte '· X años/meses'. SOLO extrae las dos fechas de esa linea.
Meses: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Campos: company, position, start_date, end_date, description

{rawText}";

        var requestBody = new
        {
            model = "gpt-5.4-nano",
            input,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "experiences",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            experiences = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    properties = new
                                    {
                                        company = new { type = "string" },
                                        position = new { type = "string" },
                                        start_date = new { type = "string" },
                                        end_date = new { type = "string" },
                                        description = new { type = "string" }
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
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/v1/responses", content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);

            var outputText = doc.RootElement
                .GetProperty("output")[0]
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString()!;

            using var resultDoc = JsonDocument.Parse(outputText);
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
