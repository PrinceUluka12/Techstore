using System.Security.Claims;
using System.Text.Json;
using TechStore.API.Data;
using TechStore.API.Models;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AuditService(AppDbContext db, IHttpContextAccessor http)
    {
        _db   = db;
        _http = http;
    }

    public async Task LogAsync(string action, string entityType, string? entityId = null,
        object? oldValues = null, object? newValues = null)
    {
        var ctx       = _http.HttpContext;
        var userIdStr = ctx?.User?.FindFirstValue("userId")
                     ?? ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userEmail = ctx?.User?.FindFirstValue(ClaimTypes.Email)
                     ?? ctx?.User?.FindFirstValue("email");
        var ip        = ctx?.Connection?.RemoteIpAddress?.ToString();

        _db.AuditLogs.Add(new AuditLog
        {
            UserId      = userIdStr != null && int.TryParse(userIdStr, out var uid) ? uid : null,
            UserEmail   = userEmail,
            Action      = action,
            EntityType  = entityType,
            EntityId    = entityId,
            OldValues   = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues   = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress   = ip,
            OccurredAt  = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
