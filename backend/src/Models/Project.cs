namespace src.Models;

public class Project
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;

    public User User { get; set; } = null!;
    public List<Keyword> Keywords { get; set; } = [];
}
