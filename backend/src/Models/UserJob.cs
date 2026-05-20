namespace src.Models;

public class UserJob
{
    public Guid UserId { get; set; }
    public int JobId { get; set; }
    public DateTime SavedAt { get; set; }
    public string? Notes { get; set; }
    public bool Applied { get; set; }
    public DateTime? AppliedAt { get; set; }

    public User User { get; set; } = null!;
    public Job Job { get; set; } = null!;
}
