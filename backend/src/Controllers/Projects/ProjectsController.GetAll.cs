using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using src.Models.DTOs;

namespace src.Controllers;

public partial class ProjectsController
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var projects = await _db.Projects
            .Where(p => p.UserId == userId)
            .Include(p => p.Keywords)
            .Select(p => new ProjectResponseDto
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Type = p.Type,
                Keywords = p.Keywords.Select(k => k.Name).ToList()
            })
            .ToListAsync();

        return Ok(new { success = true, data = projects });
    }
}
