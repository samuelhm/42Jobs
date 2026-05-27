namespace src.Models.DTOs;

public class FetchRequestDto
{
    public string? Location { get; set; }
    public int Limit { get; set; } = 10;
    public string? DatePosted { get; set; }
    public string? SortBy { get; set; }
}

public class FetchStatusDto
{
    public string Status { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public string? Error { get; set; }
}
