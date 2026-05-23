using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Data;
using src.Models;

namespace src.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public partial class ResumesController : ControllerBase
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

    private static string BuildPrompt(User user, Job job, List<WorkExperience> experiences,
        List<Education> educations, List<Project> projects, List<UserKeyword> userKeywords)
    {
        var userInfo = $@"
PROFILE:
Name: {user.Name ?? ""} {user.LastName ?? ""}
Email: {user.Email}
Phone: {user.Phone ?? ""}
Location: {user.Address ?? ""}
LinkedIn: {user.LinkedinUrl ?? ""}
GitHub: {user.GithubUrl ?? ""}
Junior: {user.Junior}
Summary: {user.Presentation ?? ""}
Languages: {string.Join(", ", user.Languages.Select(l => l.Name))}
";

        var expText = string.Join("\n", experiences.Select(e =>
            $"- {e.Position ?? ""} at {e.Company} ({e.StartDate} - {e.EndDate}): {e.Description ?? ""}. Keywords: {string.Join(", ", e.Keywords.Select(k => k.Name))}"));

        var eduText = string.Join("\n", educations.Select(e =>
            $"- {e.Degree} at {e.Institution ?? ""} ({e.StartYear} - {e.EndYear})"));

        var projText = string.Join("\n", projects.Select(p =>
            $"- {p.Name} ({p.Type}): {p.Description ?? ""}. Keywords: {string.Join(", ", p.Keywords.Select(k => k.Name))}"));

        var kwText = string.Join(", ", userKeywords
            .Where(uk => uk.LearningStatus != "not_learned")
            .Select(uk => uk.Keyword.Name));

        return $@"You are a professional CV generator optimized for ATS (Applicant Tracking Systems). Generate ONLY HTML with inline CSS. NO markdown.

JOB OFFER:
Title: {job.Title}
Company: {job.Company?.Name ?? "Not specified"}
Description: {job.Description ?? ""}
Offer keywords: {string.Join(", ", job.Keywords.Select(k => k.Name))}

{userInfo}

EXPERIENCE:
{expText}

EDUCATION:
{eduText}

PROJECTS:
{projText}

USER KEYWORDS (learned): {kwText}

CV STRUCTURE (HTML):
1. HEADER: name as h1, target job title as h2 subtitle. Contact info (email, phone, LinkedIn, GitHub) on ONE LINE separated by |, with small font. If the CV is in Spanish: profile picture on the left using URL /resources/YoFinal.webp as a round <img>. If in English: NO picture.
2. PROFILE: 3-4 lines in the offer's language, highlighting the most relevant experience for this position. Adapt the user's summary.
3. EXPERIENCE (separate section): MIN 1, MAX 3. Most relevant first. DO NOT add more than 3 under any circumstances.
4. PROJECTS (separate section): MIN 1, MAX 3. Most relevant first. DO NOT add more than 3 under any circumstances. DO NOT mention if school or personal.
5. EDUCATION: max 3, most recent first.
6. SKILLS: Grouped by category (Backend, Frontend, Databases, DevOps, AI, Tools, Soft Skills...). MIN 8 skills per category. If the offer mentions soft skills (communication, leadership, teamwork, etc.), include a Soft Skills category with at least 8 relevant soft skills. For technical categories, use the user's keywords. If fewer than 8, INFER the missing ones. NEVER invent nonsensical technologies. All lowercase except proper nouns.
7. LANGUAGES: names only (no level): English, Spanish, Catalan...

CSS: minimal, readable (Arial/Helvetica), A4-friendly, normal margins, no flashy colors. Section titles (h2) MUST be visibly larger than content text. Sections separated by subtle <hr>. NO external fonts. NO emojis.

CV LANGUAGE: same as the job offer. If the offer is in Spanish, CV in Spanish. If in English, CV in English.

FINAL REVIEW: After generating the CV, review it against the offer. If any important keywords from the offer that the user knows are missing, add them. If any sentence can be rewritten to be more ATS-friendly, do so. If any inferred skill that would improve the match is missing, include it.

Return ONLY this JSON: {{""html"": ""<complete>""}}";
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}

public class GenerateResumeDto
{
    public string? Model { get; set; }
}
