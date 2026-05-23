namespace src.Models;

public class CvTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? Css { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Resume> Resumes { get; set; } = [];
}
