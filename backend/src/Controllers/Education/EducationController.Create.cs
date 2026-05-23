using Microsoft.AspNetCore.Mvc;
using src.Models;
using src.Models.DTOs;

namespace src.Controllers;

public partial class EducationController
{
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
}
