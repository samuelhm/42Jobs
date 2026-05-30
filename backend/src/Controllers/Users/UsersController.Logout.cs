using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using src.Services;

namespace src.Controllers;

public partial class UsersController
{
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var token = Request.Cookies[_jwt.CookieName];
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (jti is not null)
                {
                    var blacklist = HttpContext.RequestServices.GetRequiredService<TokenBlacklistService>();
                    blacklist.Revoke(jti, jwt.ValidTo);
                }
            }
            catch
            {
                // If the token can't be parsed, still delete the cookie
            }
        }

        Response.Cookies.Delete(_jwt.CookieName, _jwt.GetCookieOptions());
        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logged out" });
    }
}
