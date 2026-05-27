using System.Collections.Concurrent;

namespace src.Services;

public class CvGenerationTracker
{
    private readonly ConcurrentDictionary<string, object> _inFlight = new();
    private readonly ILogger<CvGenerationTracker> _logger;

    public CvGenerationTracker(ILogger<CvGenerationTracker> logger)
    {
        _logger = logger;
    }

    public bool TryStart(Guid userId, int jobId, out string key)
    {
        key = $"{userId}:{jobId}";
        if (_inFlight.TryAdd(key, new object()))
        {
            _logger.LogDebug("CV generation lock acquired for {Key}", key);
            return true;
        }
        _logger.LogWarning("CV generation already in flight for {Key}", key);
        return false;
    }

    public void Complete(string key)
    {
        _inFlight.TryRemove(key, out _);
        _logger.LogDebug("CV generation lock released for {Key}", key);
    }
}
