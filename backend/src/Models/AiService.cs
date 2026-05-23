namespace src.Models;

public class AiService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFreeTier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<AiModel> Models { get; set; } = [];
}
