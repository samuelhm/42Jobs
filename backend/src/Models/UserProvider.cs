namespace src.Models;

public class UserProvider
{
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
