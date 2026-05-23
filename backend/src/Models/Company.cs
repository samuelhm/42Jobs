namespace src.Models;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? CompanyType { get; set; }

    public List<Job> Jobs { get; set; } = [];
}
