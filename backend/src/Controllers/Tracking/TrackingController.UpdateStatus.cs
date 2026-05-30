using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models;

namespace src.Controllers;

public partial class TrackingController
{
    [HttpPatch("{jobId:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int jobId, [FromBody] UpdateStatusDto body)
    {
        var userId = GetUserId();

        var validStatuses = new[] { "saved", "cv_enviado", "entrevista_conseguida", "empleo_conseguido", "rechazado" };
        if (!validStatuses.Contains(body.Status))
            return BadRequest(new { error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}" });

        var userJob = await _db.UserJobs
            .FirstOrDefaultAsync(uj => uj.UserId == userId && uj.JobId == jobId);

        if (userJob is null)
        {
            var jobExists = await _db.Jobs.AnyAsync(j => j.Id == jobId);
            if (!jobExists)
                return NotFound(new { error = "Job not found" });

            userJob = new UserJob { UserId = userId, JobId = jobId };
            _db.UserJobs.Add(userJob);
        }

        userJob.Status = body.Status;
        userJob.StatusUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true, status = userJob.Status, status_updated_at = userJob.StatusUpdatedAt });
    }
}

public class UpdateStatusDto
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression(@"^(saved|cv_enviado|entrevista_conseguida|empleo_conseguido|rechazado|oculto)$",
        ErrorMessage = "Status must be one of: saved, cv_enviado, entrevista_conseguida, empleo_conseguido, rechazado, oculto")]
    public string Status { get; set; } = "saved";
}
