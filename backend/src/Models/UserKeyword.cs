namespace src.Models;

public class UserKeyword
{
    public Guid UserId { get; set; }
    public int KeywordId { get; set; }
    public string LearningStatus { get; set; } = "not_learned";
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Keyword Keyword { get; set; } = null!;
}
