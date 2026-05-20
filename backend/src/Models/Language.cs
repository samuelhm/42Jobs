namespace src.Models;

public class Language
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
