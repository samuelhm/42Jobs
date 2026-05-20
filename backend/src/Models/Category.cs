namespace src.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime? LastFetchedAt { get; set; }

    public List<Job> Jobs { get; set; } = [];
    public List<UserCategory> UserCategories { get; set; } = [];
}
