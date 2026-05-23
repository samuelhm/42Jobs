namespace src.Models;

public class AiModel
{
    public int Id { get; set; }
    public int AiServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }

    public AiService AiService { get; set; } = null!;
    public List<Resume> Resumes { get; set; } = [];
}
