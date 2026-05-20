using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

[ApiController]
[Route("api/education")]
[Authorize]
public class EducationController : ControllerBase
{
    private readonly ILogger<EducationController> _logger;
    private readonly AppDbContext _db;

    public EducationController(ILogger<EducationController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var entries = await _db.Educations
            .Where(e => e.UserId == userId)
            .Select(e => new EducationDto
            {
                Id = e.Id, Degree = e.Degree, Institution = e.Institution,
                StartYear = e.StartYear, EndYear = e.EndYear
            })
            .ToListAsync();

        return Ok(new { success = true, data = entries });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EducationDto body)
    {
        var userId = GetUserId();
        var edu = new Education
        {
            UserId = userId,
            Degree = body.Degree,
            Institution = body.Institution,
            StartYear = body.StartYear,
            EndYear = body.EndYear
        };
        _db.Educations.Add(edu);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new EducationDto
            {
                Id = edu.Id, Degree = edu.Degree, Institution = edu.Institution,
                StartYear = edu.StartYear, EndYear = edu.EndYear
            }
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EducationDto body)
    {
        var userId = GetUserId();
        var edu = await _db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (edu is null) return NotFound(new { success = false, error = "Education entry not found" });

        edu.Degree = body.Degree;
        edu.Institution = body.Institution;
        edu.StartYear = body.StartYear;
        edu.EndYear = body.EndYear;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new EducationDto
            {
                Id = edu.Id, Degree = edu.Degree, Institution = edu.Institution,
                StartYear = edu.StartYear, EndYear = edu.EndYear
            }
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var userId = GetUserId();
        var edu = await _db.Educations.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (edu is null) return NotFound(new { success = false, error = "Education entry not found" });

        _db.Educations.Remove(edu);
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value!);
}
