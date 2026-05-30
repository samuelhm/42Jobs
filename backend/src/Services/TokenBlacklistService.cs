using System.Collections.Concurrent;

namespace src.Services;

public class TokenBlacklistService : IHostedService, IDisposable
{
    private readonly ConcurrentDictionary<string, DateTime> _revoked = new();
    private readonly ILogger<TokenBlacklistService> _logger;
    private Timer? _cleanupTimer;

    public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
    {
        _logger = logger;
    }

    public void Revoke(string jti, DateTime expires)
    {
        _revoked[jti] = expires;
        _logger.LogDebug("Token {Jti} revoked (expires {Expires})", jti, expires);
    }

    public bool IsRevoked(string jti)
    {
        return _revoked.ContainsKey(jti);
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _cleanupTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }

    private void CleanupExpired(object? state)
    {
        var now = DateTime.UtcNow;
        var removed = 0;

        foreach (var (jti, expires) in _revoked)
        {
            if (expires < now && _revoked.TryRemove(jti, out _))
                removed++;
        }

        if (removed > 0)
            _logger.LogDebug("Cleaned up {Count} expired token blacklist entries", removed);
    }
}
