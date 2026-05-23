namespace src.Models;

public class JobProvider
{
    public int Id { get; set; }
    public string Portal { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsEnabled { get; set; }
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? Config { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
