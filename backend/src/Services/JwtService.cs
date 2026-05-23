using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using src.Models;

namespace src.Services;

public class JwtService
{
    private readonly string _secretKey;
    private readonly string _cookieName;
    private readonly int _expirationHours;
    private readonly string _domain;
    private readonly bool _httpOnly;
    private readonly bool _secure;
    private readonly SameSiteMode _sameSite;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set");

        var jwt = configuration.GetSection("Jwt");

        _cookieName = jwt["CookieName"] ?? "42jobs_auth";
        _expirationHours = jwt.GetValue<int>("ExpirationHours", 48);
        _domain = jwt["Domain"] ?? "";
        _httpOnly = jwt.GetValue<bool>("HttpOnly", true);
        _secure = jwt.GetValue<bool>("Secure", true);
        _sameSite = jwt.GetValue<SameSiteMode>("SameSite", SameSiteMode.Lax);
    }

    public string Generate(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name ?? ""),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public CookieOptions GetCookieOptions()
    {
        var options = new CookieOptions
        {
            HttpOnly = _httpOnly,
            Secure = _secure,
            SameSite = _sameSite,
            Expires = DateTimeOffset.UtcNow.AddHours(_expirationHours),
            Path = "/"
        };

        if (!string.IsNullOrEmpty(_domain))
            options.Domain = _domain;

        return options;
    }

    public string CookieName => _cookieName;
}
