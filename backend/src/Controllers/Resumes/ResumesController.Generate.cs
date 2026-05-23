using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class ResumesController
{
    [HttpPost("{jobId:int}")]
    public async Task<IActionResult> Generate([FromRoute] int jobId, [FromBody] GenerateResumeDto? body)
    {
        var userId = GetUserId();

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

        var context = new Dictionary<string, string>
        {
            ["job_title"] = job.Title ?? "",
            ["company"] = job.Company?.Name ?? "Not specified",
            ["job_description"] = job.Description ?? "",
            ["job_keywords"] = string.Join(", ", job.Keywords.Select(k => k.Name)),
            ["user_name"] = $"{user.Name ?? ""} {user.LastName ?? ""}".Trim(),
            ["user_email"] = user.Email,
            ["user_phone"] = user.Phone ?? "",
            ["user_location"] = user.Address ?? "",
            ["user_linkedin"] = user.LinkedinUrl ?? "",
            ["user_github"] = user.GithubUrl ?? "",
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

        try
        {
            var result = await _ai.GenerateCvAsync(context);
            var cvData = result.GetProperty("html").GetString() ?? "";
            var fullJson = result.GetRawText();

            var resume = new Resume
            {
                UserId = userId,
                JobId = jobId,
                Model = body?.Model ?? "gpt-5.4-mini",
                CvData = cvData,
                JsonData = fullJson,
            };

            _db.Resumes.Add(resume);
            await _db.SaveChangesAsync();

            var userJob = await _db.UserJobs.FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == jobId);
            if (userJob is null)
            {
                _db.UserJobs.Add(new UserJob { UserId = userId, JobId = jobId, Status = "cv_enviado", StatusUpdatedAt = DateTime.UtcNow });
            }
            else
            {
                userJob.Status = "cv_enviado";
                userJob.StatusUpdatedAt = DateTime.UtcNow;
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
}
