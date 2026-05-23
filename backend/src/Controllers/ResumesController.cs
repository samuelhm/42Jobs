using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;

namespace src.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ResumesController> _logger;
    private const string DefaultModel = "gpt-5.4-mini";

    public ResumesController(AppDbContext db, IHttpClientFactory httpFactory, ILogger<ResumesController> logger)
    {
        _db = db;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    [HttpPost("{jobId:int}")]
    public async Task<IActionResult> Generate([FromRoute] int jobId, [FromBody] GenerateResumeDto? body)
    {
        var userId = GetUserId();
        var model = body?.Model ?? DefaultModel;

        var existing = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == jobId);
        if (existing is not null)
        {
            return Ok(new { success = true, id = existing.Id, cached = true, html = existing.CvData, model = existing.Model });
        }

        var user = await _db.Users
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        var job = await _db.Jobs
            .Include(j => j.Keywords)
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobId);
        if (job is null) return NotFound();

        var experiences = await _db.WorkExperiences
            .Where(w => w.UserId == userId)
            .Include(w => w.Keywords)
            .OrderByDescending(w => w.StartDate)
            .ToListAsync();

        var educations = await _db.Educations
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.StartYear)
            .ToListAsync();

        var projects = await _db.Projects
            .Where(p => p.UserId == userId)
            .Include(p => p.Keywords)
            .ToListAsync();

        var userKeywords = await _db.UserKeywords
            .Where(uk => uk.UserId == userId)
            .Include(uk => uk.Keyword)
            .ToListAsync();

        var prompt = BuildPrompt(user, job, experiences, educations, projects, userKeywords);

        try
        {
            using var http = _httpFactory.CreateClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("LLM_OPENAI_API_KEY")}");

            var requestBody = new
            {
                model,
                input = prompt,
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "cv_output",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            properties = new
                            {
                                html = new { type = "string" }
                            },
                            required = new[] { "html" },
                            additionalProperties = false
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("OpenAI request for job {JobId} with model {Model}: {Length} chars", jobId, model, json.Length);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync("https://api.openai.com/v1/responses", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("OpenAI response for job {JobId}: Status={Status}, Body={Body}", jobId, (int)response.StatusCode,
                responseBody.Length > 500 ? responseBody[..500] : responseBody);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode(500, new { error = $"OpenAI API error: {responseBody}" });
            }
            using var doc = JsonDocument.Parse(responseBody);
            var outputArr = doc.RootElement.GetProperty("output");

            string? outputText = null;
            foreach (var item in outputArr.EnumerateArray())
            {
                if (item.TryGetProperty("content", out var contentArr))
                {
                    foreach (var c in contentArr.EnumerateArray())
                    {
                        if (c.TryGetProperty("text", out var t))
                        {
                            outputText = t.GetString();
                            break;
                        }
                    }
                }
                if (outputText is not null) break;
            }

            if (outputText is null)
            {
                _logger.LogError("OpenAI response missing output text: {Body}", responseBody);
                return StatusCode(500, new { error = "Unexpected response format from OpenAI" });
            }

            using var outputDoc = JsonDocument.Parse(outputText);
            var cvData = outputDoc.RootElement;

            var resume = new Resume
            {
                UserId = userId,
                JobId = jobId,
                Model = model,
                CvData = cvData.GetProperty("html").GetString() ?? "",
            };

            _db.Resumes.Add(resume);
            await _db.SaveChangesAsync();

            var userJob = await _db.UserJobs.FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == jobId);
            if (userJob is null)
            {
                _db.UserJobs.Add(new UserJob { UserId = userId, JobId = jobId, Applied = true, AppliedAt = DateTime.UtcNow });
            }
            else
            {
                userJob.Applied = true;
                userJob.AppliedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = resume.Id, html = resume.CvData, tracked = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CV for job {JobId}", jobId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {
        var userId = GetUserId();
        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (resume is null) return NotFound();

        return Ok(new { success = true, id = resume.Id, html = resume.CvData, model = resume.Model });
    }

    [HttpGet("job/{jobId:int}")]
    public async Task<IActionResult> GetByJob([FromRoute] int jobId)
    {
        var userId = GetUserId();
        var resume = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == jobId);
        if (resume is null) return NotFound(new { error = "No CV generated for this job" });

        return Ok(new { success = true, id = resume.Id, html = resume.CvData, model = resume.Model });
    }

    private static string BuildPrompt(User user, Job job, List<WorkExperience> experiences,
        List<Education> educations, List<Project> projects, List<UserKeyword> userKeywords)
    {
        var userInfo = $@"
PERFIL:
Nombre: {user.Name ?? ""} {user.LastName ?? ""}
Email: {user.Email}
Teléfono: {user.Phone ?? ""}
Dirección: {user.Address ?? ""}
LinkedIn: {user.LinkedinUrl ?? ""}
GitHub: {user.GithubUrl ?? ""}
Junior: {user.Junior}
Presentación: {user.Presentation ?? ""}
Idiomas: {string.Join(", ", user.Languages.Select(l => l.Name))}
";

        var expText = string.Join("\n", experiences.Select(e =>
            $"- {e.Position ?? ""} en {e.Company} ({e.StartDate} - {e.EndDate}): {e.Description ?? ""}. Keywords: {string.Join(", ", e.Keywords.Select(k => k.Name))}"));

        var eduText = string.Join("\n", educations.Select(e =>
            $"- {e.Degree} en {e.Institution ?? ""} ({e.StartYear} - {e.EndYear})"));

        var projText = string.Join("\n", projects.Select(p =>
            $"- {p.Name} ({p.Type}): {p.Description ?? ""}. Keywords: {string.Join(", ", p.Keywords.Select(k => k.Name))}"));

        var kwText = string.Join(", ", userKeywords
            .Where(uk => uk.LearningStatus != "not_learned")
            .Select(uk => uk.Keyword.Name));

        return $@"Eres un generador de CVs profesionales optimizados para ATS (Applicant Tracking Systems). Genera SOLO HTML con CSS inline. SIN markdown.

OFERTA DE TRABAJO:
Título: {job.Title}
Empresa: {job.Company?.Name ?? "No especificada"}
Descripción: {job.Description ?? ""}
Keywords de la oferta: {string.Join(", ", job.Keywords.Select(k => k.Name))}

{userInfo}

EXPERIENCIA:
{expText}

EDUCACIÓN:
{eduText}

PROYECTOS:
{projText}

KEYWORDS DEL USUARIO (aprendidas): {kwText}

ESTRUCTURA DEL CV (HTML):
1. HEADER: nombre como h1, puesto ofertado como h2 subtítulo. Datos de contacto (email, teléfono, LinkedIn, GitHub) en UNA SOLA LÍNEA separados por |, con letra pequeña. Si el CV es en español: foto de perfil a la izquierda usando la URL /resources/YoFinal.webp como <img> redonda. Si es en inglés: SIN foto.
2. PERFIL: 3-4 líneas en el idioma de la oferta, destacando experiencia más relevante para este puesto. Adapta la presentación del usuario.
3. EXPERIENCIA (sección separada): MÍNIMO 1, MÁXIMO 3. Las más relevantes primero. NO agregues más de 3 bajo ningún concepto.
4. PROYECTOS (sección separada): MÍNIMO 1, MÁXIMO 3. Los más relevantes primero. NO agregues más de 3 bajo ningún concepto. NO menciones si es school o personal.
5. EDUCACIÓN: máximo 3, las más recientes primero.
6. SKILLS: Agrupadas por categorías (Backend, Frontend, Databases, DevOps, AI, Tools, Soft Skills...). MÍNIMO 8 skills por categoría. Si la oferta menciona soft skills (comunicación, liderazgo, trabajo en equipo, etc.), incluye una categoría Soft Skills con al menos 8 habilidades blandas relevantes. Para las categorías técnicas, usa las keywords del usuario. Si no llega a 8, INFIERE las que faltan. NUNCA inventes tecnologías sin sentido. Todo en minúsculas excepto nombres propios.
7. IDIOMAS: solo nombres (sin nivel): Inglés, Español, Catalán...

CSS: mínimo imprescindible, legible (Arial/Helvetica), A4-friendly, márgenes normales, sin colores estridentes. Los títulos de sección (h2) DEBEN ser visiblemente más grandes que el texto del contenido. Las secciones separadas con <hr> sutil. SIN fuentes externas. SIN emojis.

IDIOMA DEL CV: el mismo de la oferta de trabajo. Si la oferta está en español, CV en español. Si en inglés, CV en inglés.

PASO FINAL DE REVISIÓN: Después de generar el CV, revísalo contra la oferta. Si detectas que falta alguna keyword importante de la oferta que el usuario conoce, añádela. Si alguna frase puede reescribirse para ser más atractiva para un ATS, hazlo. Si ves que falta algún skill inferido que mejoraría el match, inclúyelo.

Devuelve SOLO este JSON: {{""html"": ""<completo>""}}";
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}

public class GenerateResumeDto
{
    public string? Model { get; set; }
}
