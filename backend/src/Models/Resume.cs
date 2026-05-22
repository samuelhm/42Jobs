namespace src.Models;

public class Resume
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int? JobId { get; set; }
    public string CvData { get; set; } = "";
    public string Model { get; set; } = "gpt-5.4-mini";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Job? Job { get; set; }
}
