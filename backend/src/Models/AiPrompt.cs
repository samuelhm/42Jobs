namespace src.Models;

public class AiPrompt
{
    public int Id { get; set; }
    public string Functionality { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SystemPrompt { get; set; } = string.Empty;
    public string UserPromptTemplate { get; set; } = string.Empty;
    public int? SchemaId { get; set; }
    public int? DefaultModelId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AiSchema? Schema { get; set; }
    public AiModel? DefaultModel { get; set; }
    public List<Resume> Resumes { get; set; } = [];
}
