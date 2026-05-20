using Microsoft.AspNetCore.Mvc;
using src.Models.DTOs;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch([FromRoute] Guid id, [FromBody] UpdateUserDto body)
    {
        throw new NotImplementedException();
    }
}
