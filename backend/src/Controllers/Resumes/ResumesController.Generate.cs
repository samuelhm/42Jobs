using System.Text;
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
}
