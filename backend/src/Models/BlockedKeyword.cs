namespace src.Models;

public class BlockedKeyword
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? RedirectTo { get; set; }
    public DateTime CreatedAt { get; set; }

    public Keyword? RedirectKeyword { get; set; }
}
