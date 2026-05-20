namespace src.Models;

public class WorkExperience
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Description { get; set; }

    public User User { get; set; } = null!;
    public List<Keyword> Keywords { get; set; } = [];
}
