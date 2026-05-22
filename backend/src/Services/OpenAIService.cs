using System.Text;
using System.Text.Json;
using src.Models.DTOs;

namespace src.Services;

public class OpenAIService
{
    private const string Model = "gpt-5.4-nano";
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

        var schema = new
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
                        required = new[] { "company", "position", "start_date", "end_date", "description" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "experiences" },
            additionalProperties = false
        };

        var result = await CallAsync("experiences", input, schema, ct);

        if (result.error is not null) return (new(), result.error);
        if (result.json is null) return (new(), "Empty response");

        var items = new List<LinkedInExperienceParsed>();
        if (result.json.Value.TryGetProperty("experiences", out var arr))
        {
            foreach (var item in arr.EnumerateArray())
            {
                items.Add(new LinkedInExperienceParsed
                {
                    Company = item.GetProperty("company").GetString() ?? "",
                    Position = GetString(item, "position"),
                    StartDate = GetString(item, "start_date"),
                    EndDate = GetString(item, "end_date"),
                    Description = GetString(item, "description")
                });
            }
        }
        return (items, null);
    }

    public async Task<(List<LinkedInEducationParsed> items, string? error)> ParseEducationAsync(
        string rawText, CancellationToken ct = default)
    {
        var input = $@"Extrae educacion a JSON. La linea de fechas tiene formato: 'mes. año – mes. año'.

Ej: 'ene. 2024 – may. 2025' -> start_year:2024, end_year:2025
Solo extrae el año (4 digitos).

Campos: institution, degree, start_year, end_year.
Ignora 'Aptitudes:', 'Actividades y grupos:'.

{rawText}";

        var schema = new
        {
            type = "object",
            properties = new
            {
                education = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            institution = new { type = "string" },
                            degree = new { type = "string" },
                            start_year = new { type = "integer" },
                            end_year = new { type = "integer" }
                        },
                        required = new[] { "institution", "degree", "start_year", "end_year" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "education" },
            additionalProperties = false
        };

        var result = await CallAsync("education", input, schema, ct);

        if (result.error is not null) return (new(), result.error);
        if (result.json is null) return (new(), "Empty response");

        var items = new List<LinkedInEducationParsed>();
        if (result.json.Value.TryGetProperty("education", out var arr))
        {
            foreach (var item in arr.EnumerateArray())
            {
                items.Add(new LinkedInEducationParsed
                {
                    Institution = GetString(item, "institution"),
                    Degree = item.GetProperty("degree").GetString() ?? "",
                    StartYear = GetInt(item, "start_year"),
                    EndYear = GetInt(item, "end_year")
                });
            }
        }
        return (items, null);
    }

    public async Task<(string relevante, string aptoJunior)> FilterJobRelevanceAsync(
        string keyword, string title, string? description, CancellationToken ct = default)
    {
        var input = $@"Eres un filtro de ofertas de trabajo. Responde SI o NO.

1. ¿Es RELEVANTE para un Software Engineer especializado en ""{keyword}""?
   La oferta: ""{title}""
   Descripcion: ""{description ?? "No disponible"}""
   Puestos relacionados directamente o disciplinas cercanas: SI.
   Puestos completamente no relacionados (Sales, Recruiter, etc.): NO.

2. ¿Es APTA PARA JUNIOR?
   Si pide Senior, Lead, Principal, Staff, Manager, o mas de 4 años: NO.
   Si menciona Junior, Internship, Graduate, Entry Level, o no especifica: SI.
   En caso de duda: SI.

Responde solo con este JSON: {{""relevante"": ""si/no"", ""apto_junior"": ""si/no""}}";

        var schema = new
        {
            type = "object",
            properties = new
            {
                relevante = new { type = "string" },
                apto_junior = new { type = "string" }
            },
            required = new[] { "relevante", "apto_junior" },
            additionalProperties = false
        };

        try
        {
            var result = await CallAsync("filter", input, schema, ct);
            if (result.json is not null)
            {
                var j = result.json.Value;
                return (
                    j.TryGetProperty("relevante", out var r) ? r.GetString() ?? "si" : "si",
                    j.TryGetProperty("apto_junior", out var a) ? a.GetString() ?? "si" : "si"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI filter failed, defaulting to pass");
        }

        return ("si", "si");
    }

    public async Task<(List<string> skills, string companyType)> ExtractKeywordsAsync(
        string text, CancellationToken ct = default)
    {
        var input = $@"Extrae SOLO tecnologias, lenguajes, frameworks y herramientas tecnicas.

QUE EXTRAER (ejemplos validos):
  Lenguajes: 'python', 'c#', 'javascript', 'typescript', 'go', 'rust', 'java', 'c++'
  Frameworks: 'react', 'angular', 'django', 'spring boot', '.net', 'fastapi', 'flask'
  Infra/DevOps: 'docker', 'kubernetes', 'aws', 'azure', 'gcp', 'terraform', 'jenkins', 'github actions'
  Bases de datos: 'postgresql', 'mysql', 'mongodb', 'redis', 'elasticsearch'
  Tools: 'git', 'linux', 'nginx', 'kafka', 'rabbitmq'

QUE NO EXTRAER (ignorar completamente):
  Habilidades blandas: 'comunicacion', 'trabajo en equipo', 'liderazgo'
  Requisitos academicos: 'computer science', 'grado en ingenieria'
  Frases genericas: 'deseable', 'conocimientos en', 'experiencia con'
  Conceptos abstractos: 'arquitectura' (sin especificar), 'diseno de sistemas', 'calidad'
  Texto entre parentesis: si ves 'react (deseable)', extrae solo 'react'
  Palabras sueltas ambiguas: 'data', 'cloud' (sin proveedor), 'api' (sin contexto)

NORMALIZACION:
  'node' -> 'node.js', 'js' -> 'javascript', '.net core' -> '.net'
  'ml' -> 'machine learning', 'k8s' -> 'kubernetes'
  Todo en minusculas, max 3 palabras.

Tipo empresa: Multinacional, Startup, Pyme, Consultora, No identificado.

Oferta: ""{text}""";

        var schema = new
        {
            type = "object",
            properties = new
            {
                skills = new
                {
                    type = "array",
                    items = new { type = "string" }
                },
                tipo_empresa = new { type = "string" }
            },
            required = new[] { "skills", "tipo_empresa" },
            additionalProperties = false
        };

        try
        {
            var result = await CallAsync("keywords", input, schema, ct);
            if (result.json is not null)
            {
                var j = result.json.Value;
                var skills = new List<string>();
                if (j.TryGetProperty("skills", out var arr))
                    foreach (var s in arr.EnumerateArray())
                        skills.Add(s.GetString() ?? "");

                var companyType = j.TryGetProperty("tipo_empresa", out var ctProp)
                    ? (ctProp.GetString() ?? "No identificado") : "No identificado";

                return (skills, companyType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI keyword extraction failed");
        }

        return (new(), "No identificado");
    }

    public async Task<(List<GithubProjectResult> projects, string error)> AnalyzeGithubProjectsAsync(
        string inputText, CancellationToken ct = default)
    {
        var input = $@"Analiza los repositorios de GitHub y extrae informacion estructurada.

Por cada proyecto:
1. name: nombre descriptivo (limpio, sin guiones, max 60 chars).
2. description: descripcion en castellano (2-4 frases) del proposito, tecnologias y alcance.
3. type: ""personal"" o ""school"". Si el README menciona ""42"", ""42 School"", ""cursus"", ""bootcamp"" -> school. Si no -> personal.
4. keywords: lista exhaustiva de tecnologias, lenguajes, frameworks, herramientas.

Proyectos:
{inputText}";

        var schema = new
        {
            type = "object",
            properties = new
            {
                projects = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string" },
                            description = new { type = "string" },
                            type = new { type = "string" },
                            keywords = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            }
                        },
                        required = new[] { "name", "description", "type", "keywords" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "projects" },
            additionalProperties = false
        };

        try
        {
            var result = await CallAsync("github_projects", input, schema, ct);
            if (result.json is null) return (new(), "Empty response");

            var j = result.json.Value;
            var projects = new List<GithubProjectResult>();
            if (j.TryGetProperty("projects", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var proj = new GithubProjectResult
                    {
                        Name = item.GetProperty("name").GetString() ?? "",
                        Description = item.GetProperty("description").GetString() ?? "",
                        Type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "personal" : "personal"
                    };
                    if (item.TryGetProperty("keywords", out var kwArr))
                        foreach (var kw in kwArr.EnumerateArray())
                            proj.Keywords.Add(kw.GetString() ?? "");
                    projects.Add(proj);
                }
            }
            return (projects, "");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI GitHub analysis failed");
            return (new(), ex.Message);
        }
    }

    public async Task<List<List<string>>> DedupKeywordsAsync(List<string> allKeywords, CancellationToken ct = default)
    {
        var names = string.Join("\n", allKeywords.Select(k => $"- {k}"));

        var input = $@"Agrupa palabras clave tecnicas que signifiquen lo mismo.

Reglas:
- Agrupa sinonimos exactos o muy cercanos (ej: 'js'='javascript', 'llm'='large language model', 'k8s'='kubernetes', 'ui'='ui/ux'='user interface').
- No agrupes tecnologias diferentes (ej: 'react' y 'vue' NO).
- Cada grupo en minusculas.
- Palabras sin equivalentes van en su propio grupo.

Palabras:
{names}";

        var schema = new
        {
            type = "object",
            properties = new
            {
                groups = new
                {
                    type = "array",
                    items = new
                    {
                        type = "array",
                        items = new { type = "string" }
                    }
                }
            },
            required = new[] { "groups" },
            additionalProperties = false
        };

        try
        {
            var result = await CallAsync("dedup", input, schema, ct);
            if (result.json is null) return allKeywords.Select(k => new List<string> { k }).ToList();

            var j = result.json.Value;
            var groups = new List<List<string>>();
            if (j.TryGetProperty("groups", out var arr))
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
            _logger.LogWarning(ex, "OpenAI dedup failed");
            return allKeywords.Select(k => new List<string> { k }).ToList();
        }
    }

    private async Task<(JsonElement? json, string? error)> CallAsync(
        string schemaName, string input, object schema, CancellationToken ct)
    {
        var requestBody = new
        {
            model = Model,
            input,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = schemaName,
                    strict = true,
                    schema
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
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

        var parsed = JsonDocument.Parse(outputText).RootElement;
        return (parsed, null);
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString() : null;
    }

    private static int? GetInt(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number
            ? val.GetInt32() : null;
    }
}
