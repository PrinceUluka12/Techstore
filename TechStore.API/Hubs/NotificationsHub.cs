using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Hubs;

// Live alerts for staff with the back office open (at /hubs/notifications). Only the server
// sends anything: staff answer through the API, which checks their permissions as usual.
//
// Who hears what is decided here from the database, never by the browser:
//   approvers:{shopId}  managers who can approve discounts, by their home shop
//   admins              every Admin, whatever their shop
//   the user themself   (Clients.User), e.g. the till hearing back about its own request
[Authorize(Policy = NotificationsHub.StaffOnly)]
public class NotificationsHub(AppDbContext db, IApprovalService approvals, StaffPresence presence) : Hub
{
    public const string StaffOnly = "Staff";
    public const string Admins = "admins";
    public static string ApproversOf(int storeId) => $"approvers:{storeId}";

    // Messages the browser listens for
    public const string ApprovalRequested = "ApprovalRequested"; // a till asked; the managers' alert
    public const string ApprovalUpdated   = "ApprovalUpdated";   // approved, declined, cancelled, expired or used

    public override async Task OnConnectedAsync()
    {
        var userId = int.Parse(Context.UserIdentifier!);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
        if (user != null && await approvals.CanApproveAsync(user))
        {
            if (user.StoreId is int storeId)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ApproversOf(storeId));
                presence.Join(Context.ConnectionId, storeId, userId);
            }
            if (user.Role == "Admin")
                await Groups.AddToGroupAsync(Context.ConnectionId, Admins);
        }
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        presence.Leave(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}

// Which managers have the back office open, by home shop, so a till can tell the cashier when
// nobody will see their request. It's kept in this server's memory, which is right while the
// API runs on one server; running several would need Azure SignalR Service and another way.
public class StaffPresence
{
    private readonly ConcurrentDictionary<string, (int StoreId, int UserId)> _connections = new();

    public void Join(string connectionId, int storeId, int userId) => _connections[connectionId] = (storeId, userId);
    public void Leave(string connectionId) => _connections.TryRemove(connectionId, out _);

    // People rather than connections (a manager with a phone and a laptop counts once)
    public int ManagersOnline(int storeId, int exceptUserId) => _connections.Values
        .Where(c => c.StoreId == storeId && c.UserId != exceptUserId)
        .Select(c => c.UserId).Distinct().Count();
}

// Clients.User(id) reaches every connection of that user (the same "userId" claim the controllers use)
public class UserIdClaimProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindFirst("userId")?.Value;
}
