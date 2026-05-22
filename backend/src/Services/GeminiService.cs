using System.Text;
using System.Text.Json;
using src.Models.DTOs;

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

    public async Task<(List<GithubProjectResult> projects, string error)> AnalyzeGithubProjectsAsync(
        string inputText, CancellationToken ct = default)
    {
        var prompt = $@"Eres un analizador de proyectos de GitHub. Tu tarea es analizar los repositorios de un usuario y extraer informacion estructurada de cada uno.

Por cada proyecto, debes:
1. Extraer un nombre descriptivo (limpio, sin guiones, max 60 caracteres).
2. Generar una descripcion en castellano (2-4 frases) explicando el proposito, tecnologias usadas y alcance del proyecto.
3. Determinar si es un proyecto PERSONAL o de ESCUELA/BOOTCAMP (type: ""personal"" o ""school""). Si hay README que mencione ""42"", ""42 School"", ""42 Barcelona"", ""cursus"", ""bootcamp"" → es school. Si no se puede determinar → personal.
4. Extraer una lista EXHAUSTIVA de tecnologias, lenguajes, frameworks, librerias, herramientas y conceptos tecnicos (skills). Incluye TODO lo que veas en el README, package.json, requirements.txt, Makefile, CMakeLists, docker-compose, etc. Se muy minucioso.

Proyectos a analizar:
{inputText}";

        var schema = JsonDocument.Parse("""
            {
              "type": "OBJECT",
              "properties": {
                "projects": {
                  "type": "ARRAY",
                  "items": {
                    "type": "OBJECT",
                    "properties": {
                      "name": { "type": "STRING" },
                      "description": { "type": "STRING" },
                      "type": { "type": "STRING", "enum": ["personal", "school"] },
                      "keywords": {
                        "type": "ARRAY",
                        "items": { "type": "STRING" }
                      }
                    },
                    "required": ["name", "description", "type", "keywords"]
                  }
                }
              },
              "required": ["projects"]
            }
            """).RootElement;

        try
        {
            var result = await CallGeminiAsync(prompt, schema, ct);
            var projects = new List<GithubProjectResult>();

            if (result.TryGetProperty("projects", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var proj = new GithubProjectResult();
                    if (item.TryGetProperty("name", out var n)) proj.Name = n.GetString() ?? "";
                    if (item.TryGetProperty("description", out var d)) proj.Description = d.GetString() ?? "";
                    if (item.TryGetProperty("type", out var t)) proj.Type = t.GetString() ?? "personal";
                    if (item.TryGetProperty("keywords", out var kwArr))
                    {
                        foreach (var kw in kwArr.EnumerateArray())
                            proj.Keywords.Add(kw.GetString() ?? "");
                    }
                    projects.Add(proj);
                }
            }

            return (projects, "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini GitHub analysis failed");
            return ([], ex.Message);
        }
    }

    public async Task<List<List<string>>> DedupKeywordsAsync(List<string> allKeywords, CancellationToken ct = default)
    {
        var prompt = $@"Eres un deduplicador de palabras clave técnicas. Tu tarea es agrupar palabras clave que significan EXACTAMENTE lo mismo.

Reglas:
- Agrupa términos que se refieran al mismo concepto o área, aunque no sean sinónimos exactos.
- Ejemplos de grupos válidos: 'ui' + 'ui/ux' + 'ui/ux design' + 'user interface', 'ai' + 'artificial intelligence' + 'machine learning/ai', 'aws' + 'amazon web services', 'docker' + 'docker compose' + 'containerization', 'react' + 'react.js', 'node' + 'node.js', 'python' + 'python3', 'c#' + 'csharp' + '.net'.
- No agrupes tecnologías claramente diferentes (ej: 'react' y 'vue' NO).
- Cada grupo debe tener las palabras en minúsculas.
- Si una palabra no tiene equivalentes, va en su propio grupo de 1 elemento.
- Devuelve un array de grupos, donde cada grupo es un array de strings equivalentes.

Palabras clave a analizar:
{string.Join("\n", allKeywords.Select(k => $"- {k}"))}";

        var schema = JsonDocument.Parse("""
            {
              "type": "OBJECT",
              "properties": {
                "groups": {
                  "type": "ARRAY",
                  "items": {
                    "type": "ARRAY",
                    "items": { "type": "STRING" }
                  }
                }
              },
              "required": ["groups"]
            }
            """).RootElement;

        try
        {
            var result = await CallGeminiAsync(prompt, schema, ct);
            var groups = new List<List<string>>();

            if (result.TryGetProperty("groups", out var arr))
            {
                foreach (var group in arr.EnumerateArray())
                {
                    var items = new List<string>();
                    foreach (var item in group.EnumerateArray())
                        items.Add(item.GetString() ?? "");
                    if (items.Count > 0) groups.Add(items);
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini dedup failed");
            return allKeywords.Select(k => new List<string> { k }).ToList();
        }
    }

    public async Task<(List<LinkedInExperienceParsed> items, string? error)> ParseLinkedInExperienceAsync(string rawText, CancellationToken ct = default)
    {
        var prompt = $@"Extrae experiencias laborales a JSON. La linea de fechas SIEMPRE tiene este formato exacto: 'mes. año - mes. año · X años/meses'.

Ejemplo de linea de fechas: 'sept. 2023 - ene. 2024 · 5 meses'
→ start_date: '2023-09-01', end_date: '2024-01-01'

Ejemplo: 'jun. 2020 - feb. 2023 · 2 años 9 meses'
→ start_date: '2020-06-01', end_date: '2023-02-01'

IGNORA la parte '· X años/meses'. SOLO extrae las dos fechas de esa linea.
Meses: ene=01 feb=02 mar=03 abr=04 may=05 jun=06 jul=07 ago=08 sept=09 oct=10 nov=11 dic=12

Campos: company, position, start_date, end_date, description

{rawText}";

        var schema = JsonDocument.Parse("""
            {
              "type": "OBJECT",
              "properties": {
                "experiences": {
                  "type": "ARRAY",
                  "items": {
                    "type": "OBJECT",
                    "properties": {
                      "company": { "type": "STRING" },
                      "position": { "type": "STRING" },
                      "start_date": { "type": "STRING" },
                      "end_date": { "type": "STRING" },
                      "description": { "type": "STRING" }
                    },
                    "required": ["company"]
                  }
                }
              },
              "required": ["experiences"]
            }
            """).RootElement;

        try
        {
             var result = await CallGeminiAsync(prompt, schema, ct);
            var items = new List<LinkedInExperienceParsed>();

            if (result.TryGetProperty("experiences", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    items.Add(new LinkedInExperienceParsed
                    {
                        Company = item.GetProperty("company").GetString() ?? "",
                        Position = GetNullableString(item, "position"),
                        StartDate = GetNullableString(item, "start_date"),
                        EndDate = GetNullableString(item, "end_date"),
                        Description = GetNullableString(item, "description")
                    });
                }
            }

            return (items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini LinkedIn experience parsing failed");
            return ([], ex.Message);
        }
    }

    public async Task<(List<LinkedInEducationParsed> items, string? error)> ParseLinkedInEducationAsync(string rawText, CancellationToken ct = default)
    {
        var prompt = $@"Extrae educacion a JSON. La linea de fechas tiene formato: 'mes. año – mes. año'.

Ej: 'ene. 2024 – may. 2025' → start_year:2024, end_year:2025
Ej: 'sept. 2009 – jun. 2011' → start_year:2009, end_year:2011
Solo extrae el año (4 digitos).

Campos: institution, degree, start_year, end_year.
Ignora 'Aptitudes:', 'Actividades y grupos:'.

{rawText}";

        var schema = JsonDocument.Parse("""
            {
              "type": "OBJECT",
              "properties": {
                "education": {
                  "type": "ARRAY",
                  "items": {
                    "type": "OBJECT",
                    "properties": {
                      "institution": { "type": "STRING" },
                      "degree": { "type": "STRING" },
                      "start_year": { "type": "NUMBER" },
                      "end_year": { "type": "NUMBER" }
                    },
                    "required": ["degree"]
                  }
                }
              },
              "required": ["education"]
            }
            """).RootElement;

        try
        {
            var result = await CallGeminiAsync(prompt, schema, ct);
            var items = new List<LinkedInEducationParsed>();

            if (result.TryGetProperty("education", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    items.Add(new LinkedInEducationParsed
                    {
                        Degree = item.GetProperty("degree").GetString() ?? "",
                        Institution = GetNullableString(item, "institution"),
                        StartYear = item.TryGetProperty("start_year", out var sy) && sy.ValueKind == JsonValueKind.Number ? (int?)sy.GetInt32() : null,
                        EndYear = item.TryGetProperty("end_year", out var ey) && ey.ValueKind == JsonValueKind.Number ? (int?)ey.GetInt32() : null
                    });
                }
            }

            return (items, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini LinkedIn education parsing failed");
            return ([], ex.Message);
        }
    }

    private static string? GetNullableString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var val) && val.ValueKind != JsonValueKind.Null
            ? val.GetString()
            : null;
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
