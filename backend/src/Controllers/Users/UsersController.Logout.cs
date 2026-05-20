using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        throw new NotImplementedException();
    }
}
