using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Helpers;
using TechStore.API.Models;
using TechStore.API.Repositories.Interfaces;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Once a day, from Alerts:LowStockHour (Nigeria time), staff who manage stock get one email
// listing what's at or below its low-stock level: the same items the dashboard counts.
// Admins (and staff without a home shop) see every shop; everyone else sees their own shop.
public class StockAlertService : IStockAlertService
{
    private readonly AppDbContext _db;
    private readonly IInventoryRepository _inventory;
    private readonly IEmailService _email;
    private readonly int _hour;

    public StockAlertService(AppDbContext db, IInventoryRepository inventory, IEmailService email, IConfiguration config)
    {
        _db = db;
        _inventory = inventory;
        _email = email;
        _hour = config.GetValue("Alerts:LowStockHour", 8);
    }

    // Returns how many staff were emailed
    public async Task<int> SendDailyLowStockAlertAsync(DateTime nowUtc)
    {
        var nigeria = nowUtc.AddHours(1); // UTC+1 all year
        if (nigeria.Hour < _hour) return 0;
        var today = DateOnly.FromDateTime(nigeria);
        if (await _db.JobRuns.AnyAsync(j => j.Name == JobRun.LowStockAlert && j.LastRunOn >= today)) return 0;

        // Everything is looked up before the day is marked done, so if a lookup fails the next
        // check (15 minutes later) tries again rather than finding the day already used up
        var low = (await _inventory.GetLowStockAsync()).ToList();
        var stockRoles = (await _db.AppRoles.AsNoTracking().ToListAsync())
            .Where(r => r.Permissions.Contains(Perms.InventoryManage))
            .Select(r => r.Name).ToList();
        var staff = await _db.Users.AsNoTracking()
            .Where(u => u.IsActive && (u.Role == "Admin" || stockRoles.Contains(u.Role)))
            .ToListAsync();

        // Claimed with a conditional update, so a restart or a second copy of the app can't send it twice
        var claimed = await _db.JobRuns
            .Where(j => j.Name == JobRun.LowStockAlert && j.LastRunOn < today)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LastRunOn, today)) == 1;
        if (!claimed || low.Count == 0) return 0;

        var emailed = 0;
        foreach (var person in staff)
        {
            var theirs = person.Role == "Admin" || person.StoreId == null
                ? low : low.Where(i => i.StoreId == person.StoreId).ToList();
            if (theirs.Count == 0) continue;

            await _email.SendLowStockAlertAsync(person.Email, person.FirstName, theirs
                .Select(i => new LowStockLine(i.Store.Name, i.Product.Name, i.Product.SKU,
                    i.QuantityOnHand - i.QuantityReserved, i.LowStockThreshold))
                .ToList());
            emailed++;
        }
        return emailed;
    }
}

// Checks every 15 minutes whether today's low-stock email is due (StockAlertService decides)
public class LowStockAlertJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LowStockAlertJob> _logger;

    public LowStockAlertJob(IServiceScopeFactory scopeFactory, ILogger<LowStockAlertJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var alerts = scope.ServiceProvider.GetRequiredService<IStockAlertService>();
                var emailed = await alerts.SendDailyLowStockAlertAsync(DateTime.UtcNow);
                if (emailed > 0)
                    _logger.LogInformation("Low-stock email sent to {Count} staff", emailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Low-stock alert failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
