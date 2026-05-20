using Microsoft.AspNetCore.Mvc;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
        throw new NotImplementedException();
    }
}
