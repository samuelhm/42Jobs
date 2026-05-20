using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class UsersController
{
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        throw new NotImplementedException();
    }
}
