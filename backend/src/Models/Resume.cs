namespace src.Models;

public class Resume
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int? JobId { get; set; }
    public string CvData { get; set; } = "";
    public string? JsonData { get; set; }
    public int? TemplateId { get; set; }
    public int? PromptId { get; set; }
    public int? ModelId { get; set; }
    public string Model { get; set; } = "gpt-5.4-mini";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Job? Job { get; set; }
    public CvTemplate? Template { get; set; }
    public AiPrompt? Prompt { get; set; }
    public AiModel? AiModel { get; set; }
}
