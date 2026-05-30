using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpPost("{jobId:int}")]
    [EnableRateLimiting("cv")]
    public async Task<IActionResult> Generate([FromRoute] int jobId)
    {
        var userId = GetUserId();

        var existing = await _db.Resumes.FirstOrDefaultAsync(r => r.UserId == userId && r.JobId == jobId);
        if (existing is not null)
        {
            if (Request.Query["force"] != "true")
            {
                return Ok(new { success = true, id = existing.Id, cached = true, html = existing.CvData, model = existing.Model });
            }
            _db.Resumes.Remove(existing);
            await _db.SaveChangesAsync();
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

        var template = await _db.Set<CvTemplate>()
            .FirstOrDefaultAsync(t => t.IsActive);

        var context = new Dictionary<string, string>
        {
            ["job_title"] = job.Title ?? "",
            ["company"] = job.Company?.Name ?? "Not specified",
            ["job_description"] = job.Description ?? "",
            ["job_keywords"] = string.Join(", ", job.Keywords.Select(k => k.Name)),
            ["user_presentation"] = user.Presentation ?? "",
            ["user_languages"] = string.Join(", ", user.Languages.Select(l => l.Name)),
            ["user_experiences"] = string.Join("\n", experiences.Select(e =>
                $"- {e.Position ?? ""} at {e.Company} ({e.StartDate} - {e.EndDate}): {e.Description ?? ""}. Keywords: {string.Join(", ", e.Keywords.Select(k => k.Name))}")),
            ["user_education"] = string.Join("\n", educations.Select(e =>
                $"- {e.Degree} at {e.Institution ?? ""} ({e.StartYear} - {e.EndYear})")),
            ["user_projects"] = string.Join("\n", projects.Select(p =>
                $"- {p.Name} ({p.Type}): {p.Description ?? ""}. Keywords: {string.Join(", ", p.Keywords.Select(k => k.Name))}")),
            ["user_keywords"] = string.Join(", ", userKeywords
                .Where(uk => uk.LearningStatus != "not_learned")
                .Select(uk => uk.Keyword.Name)),
        };

        if (!_cvTracker.TryStart(userId, jobId, out var lockKey))
            return StatusCode(409, new { error = "CV generation already in progress" });

        try
        {
            var cvErrors = await _readiness.CheckAsync("cv_generation");
            if (cvErrors.Count > 0)
                return StatusCode(503, new { error = string.Join("; ", cvErrors) });

            var (result, modelName) = await _ai.GenerateCvAsync(context);
            var fullJson = result.GetRawText();
            var html = RenderTemplate(template?.HtmlTemplate, user, job, result, educations);

            var resume = new Resume
            {
                UserId = userId,
                JobId = jobId,
                Model = modelName,
                CvData = html,
                JsonData = fullJson,
                TemplateId = template?.Id,
            };

            _db.Resumes.Add(resume);
            await _db.SaveChangesAsync();

            var userJob = await _db.UserJobs.FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == jobId);
            if (userJob is null)
            {
                _db.UserJobs.Add(new UserJob { UserId = userId, JobId = jobId, Status = "saved", StatusUpdatedAt = DateTime.UtcNow });
            }
            await _db.SaveChangesAsync();

            return Ok(new { success = true, id = resume.Id, html = resume.CvData, tracked = true, model = resume.Model });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CV for job {JobId}", jobId);
            return StatusCode(500, new { error = "CV generation failed" });
        }
        finally
        {
            _cvTracker.Complete(lockKey);
        }
    }

    private static string RenderTemplate(string? templateHtml, User user, Job job, JsonElement aiData, List<Education> educations)
    {
        var html = templateHtml ?? "<html><body><h1>{{name}}</h1>{{profile}}{{experiences}}{{projects}}{{education}}{{skills}}{{languages}}</body></html>";

        var replacements = new Dictionary<string, string>
        {
            ["name"] = $"{user.Name ?? ""} {user.LastName ?? ""}".Trim(),
            ["job_title"] = job.Title ?? "",
            ["company"] = job.Company?.Name ?? "",
            ["email"] = user.Email,
            ["phone"] = user.Phone ?? "",
            ["linkedin"] = user.LinkedinUrl ?? "",
            ["github"] = user.GithubUrl ?? "",
            ["location"] = user.Address ?? "",
            ["photo"] = IsSafePhotoUrl(user.Photo) ? $"<img class=\"cv-photo\" src=\"{System.Net.WebUtility.HtmlEncode(user.Photo)}\" />" : "",
            ["profile"] = aiData.TryGetProperty("profile", out var p) ? p.GetString() ?? "" : "",
            ["experiences"] = RenderExperiences(aiData),
            ["projects"] = RenderProjects(aiData),
            ["education"] = RenderEducation(educations),
            ["skills"] = RenderSkills(aiData),
            ["languages"] = string.Join(" ", user.Languages.Select(l => $"<span>{l.Name}</span>")),
        };

        foreach (var (key, value) in replacements)
            html = html.Replace($"{{{{{key}}}}}", value);

        return html;
    }

    private static bool IsSafePhotoUrl(string? photo)
    {
        if (string.IsNullOrEmpty(photo)) return false;
        return photo.StartsWith("data:image/") || photo.StartsWith("https://");
    }

    private static string RenderExperiences(JsonElement aiData)
    {
        if (!aiData.TryGetProperty("experiences", out var arr)) return "";
        var parts = new List<string>();
        foreach (var exp in arr.EnumerateArray())
        {
            var company = exp.TryGetProperty("company", out var c) ? c.GetString() ?? "" : "";
            var position = exp.TryGetProperty("position", out var pos) ? pos.GetString() ?? "" : "";
            var start = exp.TryGetProperty("start_date", out var sd) ? sd.GetString() ?? "" : "";
            var end = exp.TryGetProperty("end_date", out var ed) ? ed.GetString() ?? "" : "";
            var description = exp.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

            var descHtml = !string.IsNullOrEmpty(description)
                ? $"<div class=\"entry-desc\">{description}</div>"
                : "";

            var highlights = "";
            if (exp.TryGetProperty("highlights", out var hl))
            {
                var items = hl.EnumerateArray().Select(h => $"<li>{h.GetString()}</li>");
                highlights = $"<ul>{string.Join("", items)}</ul>";
            }

            parts.Add($@"<div class=""entry"">
  <div class=""entry-header"">{position} — {company}</div>
  <div class=""entry-dates"">{start} — {end}</div>
  {descHtml}
  {highlights}
</div>");
        }
        return string.Join("\n", parts);
    }

    private static string RenderProjects(JsonElement aiData)
    {
        if (!aiData.TryGetProperty("projects", out var arr)) return "";
        var projects = arr.EnumerateArray().ToList();
        var currentYear = DateTime.Now.Year;
        var currentYearCount = Math.Min(projects.Count, projects.Count / 2 + 1);

        var parts = new List<string>();
        for (var i = 0; i < projects.Count; i++)
        {
            var proj = projects[i];
            var name = proj.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var desc = proj.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

            var techs = "";
            if (proj.TryGetProperty("technologies", out var techArr))
            {
                var techList = techArr.EnumerateArray().Select(t => t.GetString()).Where(t => !string.IsNullOrEmpty(t));
                var joined = string.Join(", ", techList);
                if (!string.IsNullOrEmpty(joined))
                    techs = $"<div class=\"entry-tech\">{joined}</div>";
            }

            var year = i < currentYearCount ? currentYear : currentYear - 1;

            var highlights = "";
            if (proj.TryGetProperty("highlights", out var hl))
            {
                var items = hl.EnumerateArray().Select(h => $"<li>{h.GetString()}</li>");
                highlights = $"<ul>{string.Join("", items)}</ul>";
            }

            parts.Add($@"<div class=""entry"">
  <div class=""entry-header"">{name}</div>
  <div class=""entry-dates"">{year} — Present</div>
  <div class=""entry-desc"">{desc}</div>
  {techs}
  {highlights}
</div>");
        }
        return string.Join("\n", parts);
    }

    private static string RenderEducation(List<Education> educations)
    {
        var recent = educations.OrderByDescending(e => e.StartYear).Take(3);
        return string.Join("\n", recent.Select(e =>
            $@"<div class=""edu-entry"">{e.Degree} — {e.Institution ?? ""} ({e.StartYear} - {e.EndYear})</div>"));
    }

    private static string RenderSkills(JsonElement aiData)
    {
        if (!aiData.TryGetProperty("skills", out var arr)) return "";
        var parts = new List<string>();
        foreach (var group in arr.EnumerateArray())
        {
            var category = group.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "";
            var tags = "";
            if (group.TryGetProperty("items", out var items))
                tags = string.Join("", items.EnumerateArray().Select(i => $"<span class=\"skill-tag\">{i.GetString()}</span>"));

            parts.Add($@"<div class=""skill-group"">
  <h3>{category}</h3>
  <div class=""skill-tags"">{tags}</div>
</div>");
        }
        return string.Join("\n", parts);
    }
}
