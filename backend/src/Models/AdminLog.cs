namespace src.Models;

public class AdminLog
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Payload1 { get; set; }
    public string? Payload2 { get; set; }
    public string? Payload3 { get; set; }
}
