namespace src.Models;

public class UserCategory
{
    public Guid UserId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
