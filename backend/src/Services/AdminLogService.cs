using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using src.Data;
using src.Models;

namespace src.Services;

public class AdminLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminLogService> _logger;

    public AdminLogService(IServiceScopeFactory scopeFactory, ILogger<AdminLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(string actor, string action, object? payload1, string? payload2, string? payload3, string? correlationId = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.AdminLogs.Add(new AdminLog
            {
                Actor = actor,
                Action = action,
                Payload1 = payload1 is not null ? JsonSerializer.Serialize(payload1) : null,
                Payload2 = payload2,
                Payload3 = payload3,
                CorrelationId = correlationId ?? string.Empty,
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write admin log for {Actor}/{Action}: {Error}", actor, action, ex.GetBaseException().Message);
        }
    }
}
