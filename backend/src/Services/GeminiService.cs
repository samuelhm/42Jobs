using System.Text;
using System.Text.Json;

namespace src.Services;

public class GeminiService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiService> _logger;
    private const string Model = "gemini-3.1-flash-lite";

    public GeminiService(HttpClient http, ILogger<GeminiService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(string relevante, string aptoJunior)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default)
    {
        var prompt = $@"Eres un filtro de ofertas de trabajo. Tu tarea es:
1. Determinar si una oferta de trabajo es RELEVANTE para un perfil de Software Engineer especializado en ""{keyword}"".
2. Determinar si la oferta es APTA PARA UN PERFIL JUNIOR.

CRITERIOS DE RELEVANCIA:
- Puestos directamente relacionados como ""{keyword} Engineer"", ""{keyword} Developer"", etc. son relevantes.
- Puestos de disciplinas cercanas como Firmware, Embedded Systems, Hardware, IoT, RTOS, etc. (segun aplique al keyword) son relevantes.
- Puestos completamente no relacionados como ""Sales Manager"", ""Backend Developer"" (si el keyword es Embedded), ""Recruiter"", etc. NO son relevantes.
- En caso de duda, responde ""no_se"" en el campo relevante.

CRITERIOS DE PERFIL JUNIOR (apto_junior):
- Responde ""no"" si la oferta exige EXPLICITAMENTE: perfil ""Senior"", ""Senior Software Engineer"", ""Lead"", ""Principal"", ""Staff Engineer"", ""Tech Lead"", ""Engineering Manager"", o mas de 4 años de experiencia.
- Responde ""si"" si la oferta menciona ""Junior"", ""Internship"", ""Becario"", ""Graduate"", ""Entry Level"", ""Sin experiencia"", ""0-2 años"", ""1-3 años"", o no especifica nivel de seniority.
- Si la oferta pide ""3-4 años"" o ""Mid-level"" o similar, responde ""si"" (es borde pero aceptable para junior).
- Si no se menciona nada sobre seniority o años de experiencia, responde ""si"".

Oferta: ""{title}""
Descripcion: ""{description ?? "No disponible"}""";

        try
        {
            var result = await CallGeminiAsync(prompt, RelevanceSchema, ct);
            var relevante = result.GetProperty("relevante").GetString() ?? "si";
            var aptoJunior = result.GetProperty("apto_junior").GetString() ?? "si";

            _logger.LogDebug("Filter for \"{Title}\": relevante={Relevante}, apto_junior={AptoJunior}",
                title, relevante, aptoJunior);

            return (relevante, aptoJunior);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini filter failed for \"{Title}\", defaulting to pass", title);
            return ("si", "si");
        }
    }

    public async Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default)
    {
        var prompt = $@"Analiza esta oferta de trabajo y extrae las tecnologias, lenguajes, herramientas, frameworks y conceptos tecnicos mencionados. Determina tambien el tipo de empresa.

Oferta: ""{text}""";

        var result = await CallGeminiAsync(prompt, ExtractionSchema, ct);

        var skills = new List<string>();
        if (result.TryGetProperty("skills", out var skillsArray))
        {
            foreach (var skill in skillsArray.EnumerateArray())
            {
                skills.Add(skill.GetString() ?? string.Empty);
            }
        }

        var companyType = "No identificado";
        if (result.TryGetProperty("tipo_empresa", out var tipoElement))
        {
            companyType = tipoElement.GetString() ?? "No identificado";
        }

        _logger.LogDebug("Extracted {Count} keywords, company type: {Type}", skills.Count, companyType);

        return (skills, companyType);
    }

    private async Task<JsonElement> CallGeminiAsync(
        string prompt, JsonElement schema, CancellationToken ct)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
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

        var url = $"/v1beta/models/{Model}:generateContent";

        var response = await _http.PostAsync(url, content, ct);
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

    private static readonly JsonElement RelevanceSchema = JsonDocument.Parse("""
        {
          "type": "OBJECT",
          "properties": {
            "relevante": {
              "type": "STRING",
              "description": "\"si\" si la oferta es claramente relevante para el perfil, \"no\" si claramente no lo es, \"no_se\" si hay duda."
            },
            "apto_junior": {
              "type": "STRING",
              "description": "\"no\" si la oferta exige explicitamente un perfil senior, o mas de 4 años de experiencia, o un rango salarial muy alto que denota seniority. \"si\" en caso contrario."
            }
          },
          "required": ["relevante", "apto_junior"]
        }
        """).RootElement;

    private static readonly JsonElement ExtractionSchema = JsonDocument.Parse("""
        {
          "type": "OBJECT",
          "properties": {
            "skills": {
              "type": "ARRAY",
              "items": { "type": "STRING" },
              "description": "Lista exhaustiva de TODAS las tecnologias, lenguajes, frameworks, herramientas (ej. Docker, Git) y conceptos tecnicos (ej. CI/CD, SOLID) mencionados en la oferta."
            },
            "tipo_empresa": {
              "type": "STRING",
              "description": "El tipo de empresa. Solo puede ser: Multinacional, Startup, Pyme, Consultora, o \"No identificado\"."
            }
          },
          "required": ["skills", "tipo_empresa"]
        }
        """).RootElement;
}
