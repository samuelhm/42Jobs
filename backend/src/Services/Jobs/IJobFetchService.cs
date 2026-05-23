using src.Models.DTOs;

namespace src.Services.Jobs;

public interface IJobFetchService
{
    Guid? Enqueue(int categoryId, string categoryName, FetchRequestDto dto);
    FetchStatusDto? GetStatus(Guid jobId);
}
