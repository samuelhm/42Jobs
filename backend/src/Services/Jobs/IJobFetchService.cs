using src.Models.DTOs;

namespace src.Services.Jobs;

public interface IJobFetchService
{
    Guid? Enqueue(int categoryId, string categoryName, FetchRequestDto dto);
    FetchStatusDto? GetStatus(Guid jobId);
    Task FetchAllCategoriesAsync(string? datePosted = null, string? location = null);
    bool IsFetchAllRunning { get; }
}
