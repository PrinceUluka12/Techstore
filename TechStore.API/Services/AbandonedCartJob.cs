using Microsoft.EntityFrameworkCore;
using TechStore.API.Data;
using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

public class AbandonedCartJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCartJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private readonly TimeSpan _abandonThreshold = TimeSpan.FromHours(3);

    public AbandonedCartJob(IServiceScopeFactory scopeFactory, ILogger<AbandonedCartJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessAsync()
    {
        try
        {
            using var scope  = _scopeFactory.CreateScope();
            var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var cutoff = DateTime.UtcNow - _abandonThreshold;

            var carts = await db.Carts
                .Include(c => c.User)
                .Include(c => c.Items).ThenInclude(i => i.Product)
                .Where(c => c.Items.Any()
                         && c.UpdatedAt < cutoff
                         && c.AbandonedEmailSentAt == null)
                .ToListAsync();

            foreach (var cart in carts)
            {
                if (cart.User?.Email == null) continue;

                var itemNames = cart.Items
                    .Select(i => i.Product?.Name ?? "Unknown item")
                    .Take(3)
                    .ToList();

                _ = emailService.SendAbandonedCartAsync(
                    cart.User.Email, cart.User.FirstName, itemNames);

                cart.AbandonedEmailSentAt = DateTime.UtcNow;
            }

            if (carts.Count > 0)
                await db.SaveChangesAsync();

            _logger.LogInformation("AbandonedCartJob: processed {Count} carts", carts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AbandonedCartJob failed");
        }
    }
}
