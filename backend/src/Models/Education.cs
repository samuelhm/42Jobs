namespace src.Models;

public class Education
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public int? StartYear { get; set; }
    public int? EndYear { get; set; }

    public User User { get; set; } = null!;
}
