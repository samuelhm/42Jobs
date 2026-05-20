namespace src.Models;

public class Certification
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public DateOnly? DateObtained { get; set; }

    public User User { get; set; } = null!;
}
